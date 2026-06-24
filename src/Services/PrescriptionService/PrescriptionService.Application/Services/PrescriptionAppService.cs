using System.Text.Json;
using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using PrescriptionService.Domain.Entities;
using PrescriptionService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrescriptionService.Application.Services;

public sealed class PrescriptionAppService : IPrescriptionService
{
    private readonly IPrescriptionRepository _repo;
    private readonly IMedicalRecordContextClient _medicalContext;
    private readonly IMedicalRecordEventClient _medicalEvents;
    private readonly IUserPermissionClient _userPermissions;
    private readonly IPharmacyIntegrationClient _pharmacy;
    private readonly IESignatureService _signature;
    private readonly IPrescriptionValidationEngine _validation;
    private readonly IPrescriptionQrService _qr;
    private readonly IPatientInstructionGenerator _instructions;
    private readonly IPrescriptionEventPublisher _publisher;
    private readonly IEgiszClient _egisz;
    private readonly KafkaOptions _kafka;
    private readonly PrescriptionOptions _options;
    private readonly ILogger<PrescriptionAppService> _logger;

    public PrescriptionAppService(
        IPrescriptionRepository repo,
        IMedicalRecordContextClient medicalContext,
        IMedicalRecordEventClient medicalEvents,
        IUserPermissionClient userPermissions,
        IPharmacyIntegrationClient pharmacy,
        IESignatureService signature,
        IPrescriptionValidationEngine validation,
        IPrescriptionQrService qr,
        IPatientInstructionGenerator instructions,
        IPrescriptionEventPublisher publisher,
        IEgiszClient egisz,
        IOptions<KafkaOptions> kafka,
        IOptions<PrescriptionOptions> options,
        ILogger<PrescriptionAppService> logger)
    {
        _repo = repo;
        _medicalContext = medicalContext;
        _medicalEvents = medicalEvents;
        _userPermissions = userPermissions;
        _pharmacy = pharmacy;
        _signature = signature;
        _validation = validation;
        _qr = qr;
        _instructions = instructions;
        _publisher = publisher;
        _egisz = egisz;
        _kafka = kafka.Value;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PrescriptionResponse> CreateDraftAsync(
        Guid doctorId,
        CreatePrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var med in request.Medications)
        {
            if (!await _userPermissions.CanPrescribeAsync(doctorId, med.AtcCode, cancellationToken))
                throw new InvalidOperationException($"Doctor is not allowed to prescribe ATC {med.AtcCode}.");
        }

        var context = await _medicalContext.GetContextAsync(request.PatientId, cancellationToken);
        var validation = _validation.Validate(request, context, request.ConfirmWarnings);

        if (validation.Outcome == ValidationOutcome.Blocked.ToString())
            throw new InvalidOperationException(string.Join("; ", validation.Issues.Select(i => i.Message)));

        if (validation.Outcome == ValidationOutcome.RequiresConfirmation.ToString() && !request.ConfirmWarnings)
            throw new InvalidOperationException("Prescription requires doctor confirmation of warnings.");

        if (request.IsPreferential)
        {
            var egiszCheck = await _egisz.CheckPreferentialEligibilityAsync(
                request.PatientId,
                request.PreferentialCategory,
                request.Medications.Select(m => m.AtcCode).ToList(),
                cancellationToken).ConfigureAwait(false);

            if (!egiszCheck.IsEligible)
                throw new InvalidOperationException(egiszCheck.RejectionReason ?? "Patient is not eligible for preferential prescription.");
        }

        var now = DateTime.UtcNow;
        var validityDays = request.IsPreferential
            ? _options.PreferentialValidityDays
            : request.Medications.Any(m => m.RequiresPrescription)
                ? _options.ControlledValidityDays
                : _options.DefaultValidityDays;

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = doctorId,
            ConsultationId = request.ConsultationId,
            Status = PrescriptionStatus.Draft,
            IssuedAt = now,
            ValidUntil = now.AddDays(validityDays),
            IsPreferential = request.IsPreferential,
            PreferentialCategory = request.PreferentialCategory,
            DiagnosisForPrescription = request.DiagnosisForPrescription,
            PharmacistComment = request.PharmacistComment,
            AllowedRefills = request.AllowedRefills,
            AutoRenewalEnabled = request.AutoRenewalEnabled,
            ValidationResultJson = JsonSerializer.Serialize(validation),
            PatientInstructionJson = JsonSerializer.Serialize(_instructions.Generate(request.Medications)),
            CreatedAt = now,
            UpdatedAt = now,
            Medications = request.Medications.Select(MapMedication).ToList()
        };

        await _repo.SaveAsync(prescription, cancellationToken);
        await AddHistoryAsync(prescription.Id, PrescriptionStatus.Draft, PrescriptionStatus.Draft, "doctor", doctorId, null, cancellationToken);

        _logger.LogInformation("Draft prescription {Id} created for patient {PatientId}", prescription.Id, request.PatientId);
        return MapResponse(prescription, validation);
    }

    public async Task<PrescriptionValidationResultDto> ValidateAsync(
        Guid prescriptionId,
        bool confirmWarnings,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        var request = ToCreateRequest(prescription);
        var context = await _medicalContext.GetContextAsync(prescription.PatientId, cancellationToken);
        var validation = _validation.Validate(request, context, confirmWarnings);
        prescription.ValidationResultJson = JsonSerializer.Serialize(validation);
        prescription.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(prescription, cancellationToken);
        return validation;
    }

    public async Task<PrescriptionResponse> SignAsync(
        Guid prescriptionId,
        Guid doctorId,
        bool confirmWarnings,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdForUpdateAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        if (prescription.DoctorId != doctorId)
            throw new UnauthorizedAccessException("Only prescribing doctor can sign.");

        if (prescription.Status != PrescriptionStatus.Draft)
            throw new InvalidOperationException("Only draft prescriptions can be signed.");

        var validation = await ValidateAsync(prescriptionId, confirmWarnings, cancellationToken);
        if (validation.Outcome == ValidationOutcome.Blocked.ToString())
            throw new InvalidOperationException("Cannot sign blocked prescription.");
        if (validation.Outcome == ValidationOutcome.RequiresConfirmation.ToString() && !confirmWarnings)
            throw new InvalidOperationException("Confirm warnings before signing.");

        prescription.ElectronicSignature = await _signature.SignPrescriptionAsync(prescription, cancellationToken);
        prescription.SignedAt = DateTime.UtcNow;
        await TransitionAsync(prescription, PrescriptionStatus.Signed, "doctor", doctorId, null, cancellationToken);

        await _medicalEvents.AppendEventAsync(prescription.PatientId, "PrescriptionSigned", MapResponse(prescription, validation), prescription.Id, cancellationToken);
        await _publisher.PublishAsync(_kafka.PrescriptionCreatedTopic, new { prescription.Id, prescription.PatientId, prescription.DoctorId }, cancellationToken);
        await PublishStatusAsync(prescription, cancellationToken);

        return MapResponse(prescription, validation);
    }

    public async Task<PrescriptionResponse> SendToPharmacyAsync(
        Guid prescriptionId,
        Guid userId,
        SendToPharmacyRequest request,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdForUpdateAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        if (prescription.PatientId != userId && prescription.DoctorId != userId)
            throw new UnauthorizedAccessException();

        if (prescription.Status != PrescriptionStatus.Signed && prescription.Status != PrescriptionStatus.PartiallyFulfilled)
            throw new InvalidOperationException("Prescription must be signed before pharmacy dispatch.");

        var pharmacyId = request.AutoSelectNearest ? Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001") : request.PharmacyId;
        var dispatch = await _pharmacy.SendPrescriptionAsync(prescriptionId, pharmacyId, prescription, cancellationToken);

        if (!dispatch.Success)
            throw new InvalidOperationException(dispatch.Message ?? "Pharmacy rejected prescription.");

        prescription.SelectedPharmacyId = pharmacyId;
        prescription.PharmacyOrderId = dispatch.OrderId;
        await TransitionAsync(prescription, PrescriptionStatus.SentToPharmacy, "patient", userId, null, cancellationToken);
        await PublishStatusAsync(prescription, cancellationToken);

        return MapResponse(prescription, DeserializeValidation(prescription.ValidationResultJson));
    }

    public async Task<PrescriptionResponse> CancelAsync(
        Guid prescriptionId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdForUpdateAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        if (prescription.PatientId != userId && prescription.DoctorId != userId)
            throw new UnauthorizedAccessException();

        if (prescription.Status is PrescriptionStatus.Fulfilled or PrescriptionStatus.Cancelled or PrescriptionStatus.Expired)
            throw new InvalidOperationException($"Cannot cancel prescription in status {prescription.Status}.");

        await TransitionAsync(prescription, PrescriptionStatus.Cancelled, "user", userId, reason, cancellationToken);
        await PublishStatusAsync(prescription, cancellationToken);
        return MapResponse(prescription, DeserializeValidation(prescription.ValidationResultJson));
    }

    public async Task<PrescriptionResponse> RecordFulfillmentAsync(
        Guid prescriptionId,
        FulfillmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdForUpdateAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        prescription.UsedRefills += 1;
        if (!string.IsNullOrWhiteSpace(request.PharmacyOrderId))
            prescription.PharmacyOrderId = request.PharmacyOrderId;

        var target = request.Partial || prescription.UsedRefills < prescription.AllowedRefills + 1
            ? PrescriptionStatus.PartiallyFulfilled
            : PrescriptionStatus.Fulfilled;

        if (prescription.UsedRefills >= prescription.AllowedRefills + 1)
            target = PrescriptionStatus.Fulfilled;

        await TransitionAsync(prescription, target, "pharmacy", null, request.Partial ? "Partial fulfillment" : "Fulfilled", cancellationToken);
        await _publisher.PublishAsync(_kafka.PrescriptionFulfilledTopic, new { prescription.Id, request.Partial }, cancellationToken);
        await _medicalEvents.AppendEventAsync(prescription.PatientId, "PrescriptionFulfilled", new { prescription.Id, request.Partial }, prescription.Id, cancellationToken);
        await PublishStatusAsync(prescription, cancellationToken);

        return MapResponse(prescription, DeserializeValidation(prescription.ValidationResultJson));
    }

    public async Task<PrescriptionResponse> GetAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");
        return MapResponse(prescription, DeserializeValidation(prescription.ValidationResultJson));
    }

    public async Task<IReadOnlyList<PrescriptionResponse>> GetPatientPrescriptionsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetByPatientIdAsync(patientId, cancellationToken);
        return list.Select(p => MapResponse(p, DeserializeValidation(p.ValidationResultJson))).ToList();
    }

    public async Task<QrCodeResponse> GetQrCodeAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        if (string.IsNullOrWhiteSpace(prescription.ElectronicSignature))
            throw new InvalidOperationException("Prescription must be signed before QR generation.");

        return _qr.Generate(prescriptionId, prescription.ElectronicSignature);
    }

    public async Task<IReadOnlyList<PatientInstructionDto>> GetPatientInstructionsAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        var prescription = await _repo.GetByIdAsync(prescriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prescription {prescriptionId} not found.");

        return JsonSerializer.Deserialize<List<PatientInstructionDto>>(prescription.PatientInstructionJson) ?? [];
    }

    private async Task TransitionAsync(
        Prescription prescription,
        PrescriptionStatus to,
        string initiator,
        Guid? userId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var from = prescription.Status;
        prescription.Status = to;
        prescription.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(prescription, cancellationToken);
        await AddHistoryAsync(prescription.Id, from, to, initiator, userId, reason, cancellationToken);
    }

    private async Task AddHistoryAsync(
        Guid prescriptionId,
        PrescriptionStatus from,
        PrescriptionStatus to,
        string initiator,
        Guid? userId,
        string? reason,
        CancellationToken cancellationToken) =>
        await _repo.AddStatusHistoryAsync(new PrescriptionStatusHistory
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            FromStatus = from,
            ToStatus = to,
            Initiator = initiator,
            InitiatorUserId = userId,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

    private async Task PublishStatusAsync(Prescription prescription, CancellationToken cancellationToken) =>
        await _publisher.PublishAsync(_kafka.PrescriptionStatusUpdatedTopic, new
        {
            prescription.Id,
            Status = prescription.Status.ToString(),
            prescription.PatientId
        }, cancellationToken);

    private static PrescriptionMedicationItem MapMedication(MedicationItemDto dto) => new()
    {
        Id = Guid.NewGuid(),
        TradeName = dto.TradeName,
        Inn = dto.Inn,
        DosageForm = dto.DosageForm,
        Dosage = dto.Dosage,
        PackageQuantity = dto.PackageQuantity,
        Route = dto.Route,
        Frequency = dto.Frequency,
        CourseDays = dto.CourseDays,
        SpecialInstructions = dto.SpecialInstructions,
        AtcCode = dto.AtcCode,
        RequiresPrescription = dto.RequiresPrescription,
        MaxDailyDose = dto.MaxDailyDose
    };

    private static CreatePrescriptionRequest ToCreateRequest(Prescription prescription) => new()
    {
        PatientId = prescription.PatientId,
        ConsultationId = prescription.ConsultationId,
        DiagnosisForPrescription = prescription.DiagnosisForPrescription,
        IsPreferential = prescription.IsPreferential,
        PreferentialCategory = prescription.PreferentialCategory,
        AllowedRefills = prescription.AllowedRefills,
        AutoRenewalEnabled = prescription.AutoRenewalEnabled,
        PharmacistComment = prescription.PharmacistComment,
        Medications = prescription.Medications.Select(m => new MedicationItemDto
        {
            TradeName = m.TradeName,
            Inn = m.Inn,
            DosageForm = m.DosageForm,
            Dosage = m.Dosage,
            PackageQuantity = m.PackageQuantity,
            Route = m.Route,
            Frequency = m.Frequency,
            CourseDays = m.CourseDays,
            SpecialInstructions = m.SpecialInstructions,
            AtcCode = m.AtcCode,
            RequiresPrescription = m.RequiresPrescription,
            MaxDailyDose = m.MaxDailyDose
        }).ToList()
    };

    private static PrescriptionValidationResultDto? DeserializeValidation(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<PrescriptionValidationResultDto>(json);

    private static PrescriptionResponse MapResponse(Prescription p, PrescriptionValidationResultDto? validation) => new()
    {
        PrescriptionId = p.Id,
        PatientId = p.PatientId,
        DoctorId = p.DoctorId,
        ConsultationId = p.ConsultationId,
        Status = p.Status.ToString(),
        IssuedAt = p.IssuedAt,
        ValidUntil = p.ValidUntil,
        IsPreferential = p.IsPreferential,
        DiagnosisForPrescription = p.DiagnosisForPrescription,
        AllowedRefills = p.AllowedRefills,
        UsedRefills = p.UsedRefills,
        PharmacyOrderId = p.PharmacyOrderId,
        SelectedPharmacyId = p.SelectedPharmacyId,
        IsSigned = !string.IsNullOrWhiteSpace(p.ElectronicSignature),
        Medications = p.Medications.Select(m => new MedicationItemDto
        {
            TradeName = m.TradeName,
            Inn = m.Inn,
            DosageForm = m.DosageForm,
            Dosage = m.Dosage,
            PackageQuantity = m.PackageQuantity,
            Route = m.Route,
            Frequency = m.Frequency,
            CourseDays = m.CourseDays,
            SpecialInstructions = m.SpecialInstructions,
            AtcCode = m.AtcCode,
            RequiresPrescription = m.RequiresPrescription,
            MaxDailyDose = m.MaxDailyDose
        }).ToList(),
        LastValidation = validation
    };
}
