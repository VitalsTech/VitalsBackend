using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiGateway.Application.DTOs.Common;
using ApiGateway.Application.DTOs.Doctors;
using ApiGateway.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Infrastructure.Clients;

/// <summary>
/// Собирает календарь врача из четырёх сервисов. Любой недоступный источник обогащения
/// деградирует до null — сетка слотов и факт занятости возвращаются всегда.
/// </summary>
public sealed class DoctorCalendarService : IDoctorCalendarService
{
    private const int OpenSessionHorizonDays = 14;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBackendForwarder _backend;
    private readonly ILogger<DoctorCalendarService> _logger;

    public DoctorCalendarService(IBackendForwarder backend, ILogger<DoctorCalendarService> logger)
    {
        _backend = backend;
        _logger = logger;
    }

    public async Task<DoctorCalendarResponseDto> GetCalendarAsync(
        Guid doctorId,
        IReadOnlyList<Guid> doctorIdentityIds,
        DateTime? from,
        int days,
        BackendForwardContext context,
        CancellationToken cancellationToken = default)
    {
        var window = Math.Clamp(days, 1, 30);
        var start = DateTime.SpecifyKind((from ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var end = start.AddDays(window);

        var identityIds = doctorIdentityIds
            .Append(doctorId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var scheduleTask = GetAsync<ScheduleResponse>(
            "user",
            $"internal/doctors/{doctorId}/schedule?from={Uri.EscapeDataString(start.ToString("O"))}&days={window}",
            context,
            cancellationToken);

        // Незакрытые консультации показываем и вне окна, но только свежие — иначе брошенные
        // чаты копятся в каждом ответе календаря.
        var openSince = DateTime.UtcNow.AddDays(-OpenSessionHorizonDays);

        var sessionsTask = GetAsync<List<CalendarSession>>(
            "consultation",
            "internal/consultations/doctors/sessions" +
            $"?doctorIds={string.Join(',', identityIds)}" +
            $"&from={Uri.EscapeDataString(start.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(end.ToString("O"))}" +
            $"&openSince={Uri.EscapeDataString(openSince.ToString("O"))}",
            context,
            cancellationToken);

        await Task.WhenAll(scheduleTask, sessionsTask).ConfigureAwait(false);

        var schedule = scheduleTask.Result;
        var sessions = sessionsTask.Result ?? new List<CalendarSession>();

        var orphanPatientIds = (schedule?.Slots ?? new List<ScheduleSlot>())
            .Where(s => s.PatientId is not null && s.ConsultationSessionId is null)
            .Select(s => s.PatientId!.Value)
            .Distinct();

        var patientIds = sessions
            .Select(s => s.PatientId)
            .Concat(orphanPatientIds)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var patientCache = await LoadPatientContextsAsync(patientIds, context, cancellationToken).ConfigureAwait(false);
        var built = await Task.WhenAll(sessions.Select(session => BuildConsultationAsync(
            session,
            patientCache.GetValueOrDefault(session.PatientId),
            context,
            cancellationToken))).ConfigureAwait(false);

        var consultations = built
            .Where(c => c.SessionId.HasValue)
            .DistinctBy(c => c.SessionId)
            .ToDictionary(c => c.SessionId!.Value);

        var slots = new List<DoctorCalendarSlotDto>();
        var claimed = new HashSet<Guid>();

        foreach (var slot in (schedule?.Slots ?? new List<ScheduleSlot>()).OrderBy(s => s.StartsAt))
        {
            // Бронь связывает слот и сессию явно; попадание по времени — запасной вариант
            // для сессий, созданных до появления бронирования или маршрутизацией.
            var match = sessions.FirstOrDefault(s =>
                    !claimed.Contains(s.SessionId) &&
                    ((slot.Id is { } slotId && s.ScheduledSlotId == slotId) ||
                        (slot.ConsultationSessionId is { } bookedSessionId && s.SessionId == bookedSessionId)))
                ?? sessions
                    .Where(s => !claimed.Contains(s.SessionId) && s.ScheduledSlotId is null)
                    .Where(s => s.ScheduledAt >= slot.StartsAt && s.ScheduledAt < slot.EndsAt)
                    .OrderBy(s => s.ScheduledAt)
                    .FirstOrDefault();

            if (match is not null)
                claimed.Add(match.SessionId);

            var isBooked = match is not null || slot.PatientId is not null;
            DoctorCalendarConsultationDto? consultation = null;
            if (match is not null)
                consultations.TryGetValue(match.SessionId, out consultation);
            else if (slot.PatientId is { } orphanPatientId)
                consultation = BuildOrphanBookingConsultation(slot, orphanPatientId, patientCache.GetValueOrDefault(orphanPatientId));

            slots.Add(new DoctorCalendarSlotDto
            {
                Id = slot.Id,
                StartsAt = slot.StartsAt,
                EndsAt = slot.EndsAt,
                IsOnline = slot.IsOnline,
                IsAvailable = slot.IsAvailable && !isBooked,
                IsBooked = isBooked,
                Status = isBooked ? "booked" : slot.IsAvailable ? "available" : "closed",
                Consultation = consultation
            });
        }

        // Триаж для броней без сессии (обрыв book): один запрос на пациента.
        var orphanSlots = slots
            .Where(s => s.Consultation is { SessionId: null } && s.Consultation.Patient.PatientId != Guid.Empty)
            .ToList();
        if (orphanSlots.Count > 0)
        {
            var triageByPatient = new Dictionary<Guid, DoctorCalendarTriageDto?>();
            foreach (var patientId in orphanSlots.Select(s => s.Consultation!.Patient.PatientId).Distinct())
            {
                triageByPatient[patientId] = await LoadTriageAsync(patientId, null, context, cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var slot in orphanSlots)
            {
                if (triageByPatient.TryGetValue(slot.Consultation!.Patient.PatientId, out var triage))
                    slot.Consultation.Triage = triage;
            }
        }

        var unscheduled = sessions
            .Where(s => !claimed.Contains(s.SessionId))
            .OrderBy(s => s.ScheduledAt)
            .Select(s => consultations.GetValueOrDefault(s.SessionId))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        var resolvedDoctorId = schedule is not null && schedule.DoctorId != Guid.Empty
            ? schedule.DoctorId
            : doctorId;

        return new DoctorCalendarResponseDto
        {
            DoctorId = resolvedDoctorId,
            From = start,
            To = end,
            Slots = slots,
            UnscheduledConsultations = unscheduled
        };
    }

    public async Task<BookConsultationResult> BookAsync(
        Guid patientId,
        BookConsultationRequestDto request,
        BackendForwardContext context,
        CancellationToken cancellationToken = default)
    {
        var reserve = await PostAsync<ScheduleSlot>(
            "user",
            $"internal/doctors/{request.DoctorId}/schedule/slots/{request.SlotId}/reserve",
            new { patientId },
            context,
            cancellationToken).ConfigureAwait(false);

        if (reserve.Status == HttpStatusCode.NotFound)
            return new BookConsultationResult
            {
                Outcome = BookConsultationOutcome.DoctorNotFound,
                Error = "Врач или слот не найден."
            };

        if (reserve.Value is null)
            return new BookConsultationResult
            {
                Outcome = BookConsultationOutcome.SlotUnavailable,
                Error = "Слот уже занят или недоступен для записи."
            };

        var slot = reserve.Value;
        var doctor = await GetAsync<UserResponse>("user", $"api/users/{request.DoctorId}", context, cancellationToken)
            .ConfigureAwait(false);

        var session = await PostAsync<CreatedSession>(
            "consultation",
            "api/consultations",
            new
            {
                patientId,
                doctorId = request.DoctorId,
                doctorName = BuildFullName(doctor),
                consultationType = request.ConsultationType,
                urgencyLevel = request.UrgencyLevel,
                triageSessionId = request.TriageSessionId,
                scheduledAt = slot.StartsAt,
                scheduledSlotId = request.SlotId
            },
            context,
            cancellationToken).ConfigureAwait(false);

        if (session.Value is null || session.Value.SessionId == Guid.Empty)
        {
            await PostAsync<object>(
                "user",
                $"internal/doctors/schedule/slots/{request.SlotId}/release",
                new { patientId },
                context,
                cancellationToken).ConfigureAwait(false);

            return new BookConsultationResult
            {
                Outcome = BookConsultationOutcome.ConsultationFailed,
                Error = "Не удалось создать консультацию, бронь снята. Попробуйте ещё раз."
            };
        }

        var link = await PostAsync<object>(
            "user",
            $"internal/doctors/schedule/slots/{request.SlotId}/link-session",
            new { patientId, consultationSessionId = session.Value.SessionId },
            context,
            cancellationToken).ConfigureAwait(false);

        return new BookConsultationResult
        {
            Outcome = BookConsultationOutcome.Booked,
            Booking = new BookConsultationResponseDto
            {
                SessionId = session.Value.SessionId,
                DoctorId = request.DoctorId,
                PatientId = patientId,
                SlotId = request.SlotId,
                StartsAt = slot.StartsAt,
                EndsAt = slot.EndsAt,
                IsOnline = slot.IsOnline,
                Type = session.Value.Type,
                Status = session.Value.Status,
                SlotLinked = (int)link.Status is >= 200 and <= 299
            }
        };
    }

    private static string? BuildFullName(UserResponse? user)
    {
        if (user is null)
            return null;

        var fullName = string.Join(
            ' ',
            new[] { user.Surename, user.FirstName, user.SecondName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private async Task<Dictionary<Guid, PatientContext>> LoadPatientContextsAsync(
        IReadOnlyList<Guid> patientIds,
        BackendForwardContext context,
        CancellationToken cancellationToken)
    {
        if (patientIds.Count == 0)
            return new Dictionary<Guid, PatientContext>();

        var loaded = await Task.WhenAll(patientIds.Select(async patientId =>
        {
            var userTask = GetAsync<UserResponse>("user", $"api/users/{patientId}", context, cancellationToken);
            var stateTask = GetAsync<PatientStateResponse>(
                "medical",
                $"internal/medical-records/patients/{patientId}/state",
                context,
                cancellationToken);

            await Task.WhenAll(userTask, stateTask).ConfigureAwait(false);

            return (patientId, ctx: new PatientContext(userTask.Result, stateTask.Result));
        })).ConfigureAwait(false);

        return loaded.ToDictionary(x => x.patientId, x => x.ctx);
    }

    private async Task<DoctorCalendarConsultationDto> BuildConsultationAsync(
        CalendarSession session,
        PatientContext? patientContext,
        BackendForwardContext context,
        CancellationToken cancellationToken)
    {
        var triage = await LoadTriageAsync(session.PatientId, session.TriageSessionId, context, cancellationToken)
            .ConfigureAwait(false);

        return new DoctorCalendarConsultationDto
        {
            SessionId = session.SessionId,
            Type = session.Type,
            Status = session.Status,
            IsOpen = session.IsOpen,
            UrgencyLevel = session.UrgencyLevel,
            ExpectedDurationMinutes = session.ExpectedDurationMinutes,
            ScheduledAt = session.ScheduledAt,
            CreatedAt = session.CreatedAt,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            LastActivityAt = session.LastActivityAt,
            UnreadCount = session.DoctorUnreadCount,
            VideoRoomId = session.VideoRoomId,
            Patient = MapPatient(session.PatientId, patientContext?.User),
            Triage = triage,
            Anamnesis = MapAnamnesis(patientContext?.State)
        };
    }

    /// <summary>
    /// Слот забронирован, но сессия не создалась — всё равно отдаём пациента и анамнез.
    /// Триаж догружается отдельным проходом в GetCalendarAsync.
    /// </summary>
    private static DoctorCalendarConsultationDto BuildOrphanBookingConsultation(
        ScheduleSlot slot,
        Guid patientId,
        PatientContext? patientContext) =>
        new()
        {
            SessionId = null,
            Type = string.Empty,
            Status = "BookedPendingSession",
            IsOpen = false,
            UrgencyLevel = 0,
            ExpectedDurationMinutes = Math.Max(1, (int)(slot.EndsAt - slot.StartsAt).TotalMinutes),
            ScheduledAt = slot.StartsAt,
            CreatedAt = slot.BookedAt ?? slot.StartsAt,
            StartedAt = null,
            CompletedAt = null,
            LastActivityAt = slot.BookedAt ?? slot.StartsAt,
            UnreadCount = 0,
            VideoRoomId = null,
            Patient = MapPatient(patientId, patientContext?.User),
            Triage = null,
            Anamnesis = MapAnamnesis(patientContext?.State)
        };

    private async Task<DoctorCalendarTriageDto?> LoadTriageAsync(
        Guid patientId,
        Guid? triageSessionId,
        BackendForwardContext context,
        CancellationToken cancellationToken)
    {
        if (triageSessionId is { } id && id != Guid.Empty)
        {
            var direct = await GetAsync<TriageSessionResponse>(
                "triage",
                $"internal/triage/sessions/{id}",
                context,
                cancellationToken).ConfigureAwait(false);

            if (direct is not null)
                return MapTriage(direct);
        }

        // Триаж мог быть сохранён под ProfileId, а консультация — под PublicId (и наоборот).
        var aliases = await ResolvePatientIdentityIdsAsync(patientId, context, cancellationToken)
            .ConfigureAwait(false);
        var also = string.Join(',', aliases.Where(a => a != patientId));
        var path = string.IsNullOrEmpty(also)
            ? $"internal/triage/patients/{patientId}/sessions?limit=1"
            : $"internal/triage/patients/{patientId}/sessions?limit=1&alsoPatientIds={Uri.EscapeDataString(also)}";

        var latest = await GetAsync<List<TriageSessionResponse>>(
            "triage",
            path,
            context,
            cancellationToken).ConfigureAwait(false);

        var fallback = latest?.FirstOrDefault();
        return fallback is null ? null : MapTriage(fallback);
    }

    private async Task<List<Guid>> ResolvePatientIdentityIdsAsync(
        Guid patientId,
        BackendForwardContext context,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid> { patientId };
        var resolved = await GetAsync<IdentityIdsResponse>(
            "user",
            $"internal/users/{patientId}/identity-ids",
            context,
            cancellationToken).ConfigureAwait(false);

        if (resolved?.IdentityIds is { Count: > 0 })
        {
            foreach (var id in resolved.IdentityIds)
            {
                if (id != Guid.Empty)
                    ids.Add(id);
            }
        }

        return ids.ToList();
    }

    private static DoctorCalendarPatientDto MapPatient(Guid patientId, UserResponse? user)
    {
        var patient = new DoctorCalendarPatientDto { PatientId = patientId };
        if (user is null)
            return patient;

        var fullName = string.Join(
            ' ',
            new[] { user.Surename, user.FirstName, user.SecondName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        patient.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName;
        patient.Sex = string.IsNullOrWhiteSpace(user.Sex) ? null : user.Sex;
        patient.Age = CalculateAge(user.BirthDate);
        return patient;
    }

    private static int? CalculateAge(DateTime birthDate)
    {
        if (birthDate == default)
            return null;

        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age is >= 0 and < 130 ? age : null;
    }

    private static DoctorCalendarTriageDto MapTriage(TriageSessionResponse triage)
    {
        var assessment = triage.LatestAssessment;
        var symptoms = (assessment?.ExtractedEntities ?? new List<ExtractedEntity>())
            .Select(e => e.Value)
            .Concat((assessment?.NerEntities ?? new List<NerEntity>())
                .Select(e => e.NormalizedTerm ?? e.Text))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        return new DoctorCalendarTriageDto
        {
            SessionId = triage.SessionId,
            Status = triage.Status,
            UrgencyLevel = triage.LatestUrgencyLevel,
            Urgency = triage.Urgency,
            UrgencyLabel = MapUrgencyLabel(triage.LatestUrgencyLevel),
            RecommendedSpecialization = triage.RecommendedSpecialization,
            Recommendation = triage.Recommendation ?? triage.RecommendationText,
            CanBeRemote = triage.CanBeRemote,
            Complaints = triage.Messages
                ?.FirstOrDefault(m => m.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                ?.Content,
            Symptoms = symptoms,
            Hypotheses = (assessment?.LlmResult?.Hypotheses ?? new List<Hypothesis>())
                .Select(h => new DoctorCalendarHypothesisDto
                {
                    Condition = h.Condition,
                    Probability = h.Probability
                })
                .ToList(),
            EmergencyWarning = assessment?.LlmResult?.EmergencyWarning ?? false,
            CreatedAt = triage.CreatedAt
        };
    }

    private static string MapUrgencyLabel(int level) => level switch
    {
        >= 5 => "Экстренно",
        4 => "Срочно",
        3 => "В течение суток",
        2 => "Плановое обращение",
        _ => "Самонаблюдение"
    };

    private static DoctorCalendarAnamnesisDto? MapAnamnesis(PatientStateResponse? state)
    {
        if (state is null)
            return null;

        var diagnoses = (state.ActiveDiagnoses ?? new List<Diagnosis>())
            .Select(d => string.IsNullOrWhiteSpace(d.Icd10Code) ? d.Description : $"{d.Icd10Code} — {d.Description}")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var medications = (state.ActivePrescriptions ?? new List<Prescription>())
            .Select(p => string.IsNullOrWhiteSpace(p.Dosage) ? p.MedicationName : $"{p.MedicationName}, {p.Dosage}")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var allergies = (state.Allergies ?? new List<Allergy>())
            .Select(a => string.IsNullOrWhiteSpace(a.Severity) ? a.Allergen : $"{a.Allergen} ({a.Severity})")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var labs = (state.RecentLabResults ?? new List<LabResult>())
            .Select(l => l.IsCritical ? $"{l.TestName}: {l.ResultValue} (критично)" : $"{l.TestName}: {l.ResultValue}")
            .ToList();

        var vital = state.LatestVital is null
            ? null
            : $"{state.LatestVital.VitalType}: {state.LatestVital.Value} {state.LatestVital.Unit}".Trim();

        return new DoctorCalendarAnamnesisDto
        {
            ActiveDiagnoses = diagnoses,
            ActiveMedications = medications,
            Allergies = allergies,
            RecentLabResults = labs,
            LatestVital = vital,
            HasData = diagnoses.Count > 0 || medications.Count > 0 || allergies.Count > 0 || labs.Count > 0 || vital is not null
        };
    }

    private async Task<T?> GetAsync<T>(
        string service,
        string path,
        BackendForwardContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _backend
                .ForwardAsync(service, HttpMethod.Get, path, context, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Doctor calendar: {Service} {Path} returned {StatusCode}",
                    service,
                    path,
                    (int)response.StatusCode);
                return default;
            }

            return await response.Content
                .ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Doctor calendar: {Service} {Path} failed", service, path);
            return default;
        }
    }

    private async Task<(HttpStatusCode Status, T? Value)> PostAsync<T>(
        string service,
        string path,
        object? body,
        BackendForwardContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _backend
                .ForwardJsonAsync(service, HttpMethod.Post, path, context, body, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Doctor calendar: POST {Service} {Path} returned {StatusCode}",
                    service,
                    path,
                    (int)response.StatusCode);
                return (response.StatusCode, default);
            }

            // Chunked ответы часто без ContentLength — нельзя считать null пустым телом.
            // 204 No Content / реально пустой буфер — норма для release/link.
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            if (bytes.Length == 0)
                return (response.StatusCode, default);

            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            return (response.StatusCode, value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Doctor calendar: POST {Service} {Path} failed", service, path);
            return (HttpStatusCode.InternalServerError, default);
        }
    }

    private sealed record PatientContext(UserResponse? User, PatientStateResponse? State);

    private sealed class CreatedSession
    {
        public Guid SessionId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private sealed class ScheduleResponse
    {
        public Guid DoctorId { get; set; }
        public List<ScheduleSlot> Slots { get; set; } = new();
    }

    private sealed class ScheduleSlot
    {
        public Guid? Id { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsOnline { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? ConsultationSessionId { get; set; }
        public DateTime? BookedAt { get; set; }
    }

    private sealed class CalendarSession
    {
        public Guid SessionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public int UrgencyLevel { get; set; }
        public int ExpectedDurationMinutes { get; set; }
        public Guid? TriageSessionId { get; set; }
        public Guid? ScheduledSlotId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public int DoctorUnreadCount { get; set; }
        public string? VideoRoomId { get; set; }
    }

    private sealed class UserResponse
    {
        public string FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string Surename { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string? Sex { get; set; }
    }

    private sealed class IdentityIdsResponse
    {
        public List<Guid>? IdentityIds { get; set; }
    }

    private sealed class TriageSessionResponse
    {
        public Guid SessionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LatestUrgencyLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TriageMessage>? Messages { get; set; }
        public TriageAssessment? LatestAssessment { get; set; }
        public string? Urgency { get; set; }
        public string? Recommendation { get; set; }
        public string? RecommendationText { get; set; }
        public string? RecommendedSpecialization { get; set; }
        public bool CanBeRemote { get; set; }
    }

    private sealed class TriageMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class TriageAssessment
    {
        public List<ExtractedEntity>? ExtractedEntities { get; set; }
        public List<NerEntity>? NerEntities { get; set; }
        public LlmTriageResult? LlmResult { get; set; }
    }

    private sealed class ExtractedEntity
    {
        public string Type { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private sealed class NerEntity
    {
        public string? Text { get; set; }
        public string? NormalizedTerm { get; set; }
    }

    private sealed class LlmTriageResult
    {
        public List<Hypothesis>? Hypotheses { get; set; }
        public bool EmergencyWarning { get; set; }
    }

    private sealed class Hypothesis
    {
        public string Condition { get; set; } = string.Empty;
        public double Probability { get; set; }
    }

    private sealed class PatientStateResponse
    {
        public List<Diagnosis>? ActiveDiagnoses { get; set; }
        public List<Prescription>? ActivePrescriptions { get; set; }
        public List<Allergy>? Allergies { get; set; }
        public VitalSign? LatestVital { get; set; }
        public List<LabResult>? RecentLabResults { get; set; }
    }

    private sealed class Diagnosis
    {
        public string? Icd10Code { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private sealed class Prescription
    {
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
    }

    private sealed class Allergy
    {
        public string Allergen { get; set; } = string.Empty;
        public string? Severity { get; set; }
    }

    private sealed class VitalSign
    {
        public string VitalType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Unit { get; set; }
    }

    private sealed class LabResult
    {
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
    }
}
