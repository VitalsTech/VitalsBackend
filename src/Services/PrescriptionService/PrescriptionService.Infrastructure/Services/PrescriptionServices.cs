using System.Security.Cryptography;
using System.Text;
using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using Microsoft.Extensions.Options;
using QRCoder;

namespace PrescriptionService.Infrastructure.Services;

public sealed class PrescriptionQrService : IPrescriptionQrService
{
    private readonly PrescriptionOptions _options;

    public PrescriptionQrService(IOptions<PrescriptionOptions> options) => _options = options.Value;

    public QrCodeResponse Generate(Guid prescriptionId, string signature)
    {
        var payload = $"{prescriptionId}|{signature}|{ComputeHmac(prescriptionId, signature)}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(20);

        return new QrCodeResponse
        {
            PrescriptionId = prescriptionId.ToString(),
            Payload = payload,
            QrCodeBase64Png = Convert.ToBase64String(png)
        };
    }

    private string ComputeHmac(Guid prescriptionId, string signature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.QrSigningSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{prescriptionId}:{signature}"));
        return Convert.ToBase64String(hash);
    }
}

public sealed class TemplatePatientInstructionGenerator : IPatientInstructionGenerator
{
    public IReadOnlyList<PatientInstructionDto> Generate(IReadOnlyList<MedicationItemDto> medications) =>
        medications.Select(m => new PatientInstructionDto
        {
            MedicationName = m.TradeName,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            TimingRelativeToFood = InferFoodTiming(m.SpecialInstructions),
            CourseDays = m.CourseDays,
            SpecialNotes = BuildNotes(m),
            Warnings = BuildWarnings(m)
        }).ToList();

    private static string InferFoodTiming(string? special) =>
        special?.Contains("до еды", StringComparison.OrdinalIgnoreCase) == true ? "За 30 минут до еды"
        : special?.Contains("после еды", StringComparison.OrdinalIgnoreCase) == true ? "После еды"
        : special?.Contains("во время еды", StringComparison.OrdinalIgnoreCase) == true ? "Во время еды"
        : "По указанию врача";

    private static IReadOnlyList<string> BuildNotes(MedicationItemDto m)
    {
        var notes = new List<string> { $"Способ применения: {m.Route}" };
        if (!string.IsNullOrWhiteSpace(m.SpecialInstructions))
            notes.Add(m.SpecialInstructions);
        notes.Add("Запивать водой, если не указано иное.");
        return notes;
    }

    private static IReadOnlyList<string> BuildWarnings(MedicationItemDto m)
    {
        var warnings = new List<string>();
        if (m.AtcCode.StartsWith("N", StringComparison.OrdinalIgnoreCase))
            warnings.Add("Может вызывать сонливость — не управляйте автомобилем.");
        warnings.Add("Не сочетать с алкоголем без согласования с врачом.");
        return warnings;
    }
}
