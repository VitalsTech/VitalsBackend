using ConsultationService.Application.Options;
using ConsultationService.Domain.Enums;

namespace ConsultationService.Application.Services;

internal static class SessionLifecycle
{
    public static bool CanTransition(ConsultationStatus from, ConsultationStatus to) =>
        (from, to) switch
        {
            (ConsultationStatus.Created, ConsultationStatus.PatientJoined) => true,
            (ConsultationStatus.Created, ConsultationStatus.DoctorJoined) => true,
            (ConsultationStatus.Created, ConsultationStatus.Cancelled) => true,
            (ConsultationStatus.Created, ConsultationStatus.Expired) => true,
            (ConsultationStatus.PatientJoined, ConsultationStatus.DoctorJoined) => true,
            (ConsultationStatus.PatientJoined, ConsultationStatus.Active) => true,
            (ConsultationStatus.PatientJoined, ConsultationStatus.Cancelled) => true,
            (ConsultationStatus.PatientJoined, ConsultationStatus.Expired) => true,
            (ConsultationStatus.DoctorJoined, ConsultationStatus.Expired) => true,
            (ConsultationStatus.DoctorJoined, ConsultationStatus.Active) => true,
            (ConsultationStatus.DoctorJoined, ConsultationStatus.PatientJoined) => true,
            (ConsultationStatus.Active, ConsultationStatus.Paused) => true,
            (ConsultationStatus.Active, ConsultationStatus.DoctorLeft) => true,
            (ConsultationStatus.Active, ConsultationStatus.Completed) => true,
            (ConsultationStatus.Paused, ConsultationStatus.Active) => true,
            (ConsultationStatus.DoctorLeft, ConsultationStatus.Completed) => true,
            (ConsultationStatus.DoctorLeft, ConsultationStatus.Active) => true,
            (_, ConsultationStatus.Cancelled) when from is not ConsultationStatus.Completed and not ConsultationStatus.Cancelled => true,
            _ => false
        };

    public static ConsultationType ParseType(string? format) =>
        Enum.TryParse<ConsultationType>(format, true, out var parsed) ? parsed : ConsultationType.SyncChat;

    public static int DefaultDurationMinutes(ConsultationType type, ConsultationOptions options) =>
        type == ConsultationType.Video ? options.DefaultVideoDurationMinutes : options.DefaultChatDurationMinutes;
}
