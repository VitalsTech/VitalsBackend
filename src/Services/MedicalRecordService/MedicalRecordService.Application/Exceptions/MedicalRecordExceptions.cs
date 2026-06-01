namespace MedicalRecordService.Application.Exceptions;

public class MedicalRecordValidationException : Exception
{
    public MedicalRecordValidationException(string message) : base(message) { }
}

public class AccessDeniedException : Exception
{
    public AccessDeniedException(string message = "Access to medical record denied.") : base(message) { }
}

public class DuplicateEventException : Exception
{
    public DuplicateEventException(Guid eventId) : base($"Event {eventId} already exists.") { }
}
