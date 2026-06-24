namespace ConsultationService.Domain.Enums;

public enum ConsultationStatus
{
    Created = 1,
    PatientJoined = 2,
    DoctorJoined = 3,
    Active = 4,
    Paused = 5,
    DoctorLeft = 6,
    Completed = 7,
    Cancelled = 8,
    Expired = 9
}
