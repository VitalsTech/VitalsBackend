using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Enums;
using MedicalRecordService.Domain.Interfaces;
using Vitals.ObjectStorage;

namespace MedicalRecordService.Infrastructure.Services;

public sealed class PatientAttachmentService : IPatientAttachmentService
{
    private readonly IPatientAttachmentRepository _attachments;
    private readonly IAccessControlService _access;
    private readonly IObjectStorageProvider _storage;

    public PatientAttachmentService(
        IPatientAttachmentRepository attachments,
        IAccessControlService access,
        IObjectStorageProvider storage)
    {
        _attachments = attachments;
        _access = access;
        _storage = storage;
    }

    public async Task<PatientAttachmentDto> UploadAsync(
        Guid patientId,
        string fileName,
        Stream content,
        string contentType,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _access.EnsureAccessAsync(patientId, actor, AccessScopes.WriteEvents, cancellationToken);

        var objectKey = $"patients/{patientId}/{Guid.NewGuid():N}/{fileName}";
        var stored = await _storage.UploadAsync(new StoreObjectRequest
        {
            ObjectKey = objectKey,
            Content = content,
            ContentType = contentType
        }, cancellationToken);

        var attachment = new PatientAttachment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UploadedByUserId = actor.UserId,
            FileName = fileName,
            ContentType = contentType,
            ObjectKey = stored.ObjectKey,
            Url = stored.Url,
            CdnUrl = stored.CdnUrl,
            SizeBytes = stored.SizeBytes,
            UploadedAt = DateTime.UtcNow
        };

        await _attachments.AddAsync(attachment, cancellationToken);
        return Map(attachment);
    }

    public async Task<IReadOnlyList<PatientAttachmentDto>> ListAsync(
        Guid patientId,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _access.EnsureAccessAsync(patientId, actor, AccessScopes.ReadHistory, cancellationToken);
        var items = await _attachments.GetByPatientIdAsync(patientId, cancellationToken);
        return items.Select(Map).ToList();
    }

    private static PatientAttachmentDto Map(PatientAttachment attachment) => new()
    {
        Id = attachment.Id,
        PatientId = attachment.PatientId,
        FileName = attachment.FileName,
        ContentType = attachment.ContentType,
        Url = attachment.CdnUrl ?? attachment.Url,
        CdnUrl = attachment.CdnUrl,
        SizeBytes = attachment.SizeBytes,
        UploadedAt = attachment.UploadedAt
    };
}
