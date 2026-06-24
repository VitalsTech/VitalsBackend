namespace AITriageService.Application.Exceptions;

public class TriageNotFoundException : Exception
{
    public TriageNotFoundException(Guid sessionId) : base($"Triage session {sessionId} not found.") { }
}

public class TriageValidationException : Exception
{
    public TriageValidationException(string message) : base(message) { }
}
