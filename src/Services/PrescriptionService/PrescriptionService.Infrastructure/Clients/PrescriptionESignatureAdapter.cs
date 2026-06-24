using System.Text;
using System.Text.Json;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Domain.Entities;
using Vitals.ESignature;

namespace PrescriptionService.Infrastructure.Clients;

public sealed class PrescriptionESignatureAdapter : IESignatureService
{
    private readonly IESignatureProvider _provider;

    public PrescriptionESignatureAdapter(IESignatureProvider provider) => _provider = provider;

    public async Task<string> SignPrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.Serialize(new
        {
            prescription.Id,
            prescription.PatientId,
            prescription.DoctorId,
            Medications = prescription.Medications.Select(m => new { m.AtcCode, m.TradeName, m.Dosage, m.Frequency }).ToList(),
            prescription.SignedAt
        });

        var result = await _provider.SignAsync(new SignDocumentRequest
        {
            DocumentType = "prescription",
            DocumentId = prescription.Id,
            SignerId = prescription.DoctorId,
            ContentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content))
        }, cancellationToken).ConfigureAwait(false);

        return result.Signature;
    }
}
