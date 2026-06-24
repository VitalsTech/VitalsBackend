using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Interfaces;
using MedicalRecordService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Infrastructure.Repositories;

public sealed class PatientAttachmentRepository : IPatientAttachmentRepository
{
    private readonly MedicalRecordDbContext _db;

    public PatientAttachmentRepository(MedicalRecordDbContext db) => _db = db;

    public async Task AddAsync(PatientAttachment attachment, CancellationToken cancellationToken = default)
    {
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PatientAttachment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        _db.Attachments.AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<PatientAttachment>)t.Result, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
