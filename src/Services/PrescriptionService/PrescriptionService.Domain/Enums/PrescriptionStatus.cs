namespace PrescriptionService.Domain.Enums;

public enum PrescriptionStatus
{
    Draft = 1,
    Signed = 2,
    SentToPharmacy = 3,
    Fulfilled = 4,
    PartiallyFulfilled = 5,
    Expired = 6,
    Cancelled = 7
}
