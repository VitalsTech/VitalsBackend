namespace ApiGateway.Application.DTOs.Prescriptions;

public sealed class LabOrderItemDto
{
    public Guid? ItemId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? TestCode { get; set; }
    public string? SpecimenType { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? ResultValue { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public bool IsCritical { get; set; }
    public DateTime? ResultReceivedAt { get; set; }
    public string? ResultAttachmentUrl { get; set; }
    public string? ResultComment { get; set; }
}

public sealed class CreateLabOrderRequestDto
{
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public string? ClinicalIndication { get; set; }
    /// <summary>routine | urgent</summary>
    public string Priority { get; set; } = "routine";
    public string? DoctorComment { get; set; }
    public IReadOnlyList<LabOrderItemDto> Items { get; set; } = Array.Empty<LabOrderItemDto>();
}

public sealed class CancelLabOrderRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class StartLabOrderRequestDto
{
    public string? ExternalLabOrderId { get; set; }
}

public sealed class LabOrderResultItemDto
{
    public Guid ItemId { get; set; }
    public string? ResultValue { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public bool IsCritical { get; set; }
    public string? ResultAttachmentUrl { get; set; }
    public string? ResultComment { get; set; }
}

public sealed class RecordLabOrderResultsRequestDto
{
    public bool MarkCompleted { get; set; } = true;
    public IReadOnlyList<LabOrderResultItemDto> Results { get; set; } = Array.Empty<LabOrderResultItemDto>();
}
