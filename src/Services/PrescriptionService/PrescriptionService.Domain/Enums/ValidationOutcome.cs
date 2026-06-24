namespace PrescriptionService.Domain.Enums;

public enum ValidationOutcome
{
    Allowed = 1,
    RequiresConfirmation = 2,
    Blocked = 3
}
