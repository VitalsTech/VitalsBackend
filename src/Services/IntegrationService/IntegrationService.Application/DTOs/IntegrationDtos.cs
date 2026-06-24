namespace IntegrationService.Application.DTOs;

public sealed class DispatchSmsRequest
{
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class DispatchEmailRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
}

public sealed class DispatchPushRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Platform { get; set; } = "fcm";
}

public sealed class PharmacyOrderRequest
{
    public Guid PrescriptionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid PharmacyId { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public sealed class LabOrderRequest
{
    public Guid PatientId { get; set; }
    public Guid SessionId { get; set; }
    public IReadOnlyList<string> Labs { get; set; } = Array.Empty<string>();
    public string Priority { get; set; } = "planned";
}

public sealed class EmergencyDispatchRequest
{
    public Guid PatientId { get; set; }
    public Guid SessionId { get; set; }
    public string? Symptoms { get; set; }
    public int UrgencyLevel { get; set; }
}

public sealed class IntegrationDispatchResponse
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = "accepted";
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class PaymentProcessRequest
{
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string? PaymentMethodToken { get; set; }
}

public sealed class PaymentProcessResponse
{
    public string Status { get; set; } = "completed";
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class VoiceCallRequest
{
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int UrgencyLevel { get; set; } = 1;
}

public sealed class EgiszPreferentialCheckRequest
{
    public Guid PatientId { get; set; }
    public string? Snils { get; set; }
    public string? PreferentialCategory { get; set; }
    public IReadOnlyList<string> AtcCodes { get; set; } = Array.Empty<string>();
}

public sealed class EgiszPreferentialCheckResponse
{
    public bool IsEligible { get; set; }
    public string? RejectionReason { get; set; }
    public string? EgiszReference { get; set; }
}

public sealed class StorageUploadResponse
{
    public string ObjectKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? CdnUrl { get; set; }
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
}
