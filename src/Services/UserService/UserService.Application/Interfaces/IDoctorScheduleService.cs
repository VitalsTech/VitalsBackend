using UserService.Application.DTOs.Doctor;

namespace UserService.Application.Interfaces;

public interface IDoctorScheduleService
{
    Task<DoctorSearchResponseDto> SearchDoctorsAsync(DoctorSearchRequest request, CancellationToken cancellationToken = default);
    Task<DoctorScheduleResponseDto> GetScheduleAsync(Guid doctorPublicId, DateTime? from, int days, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotDto> UpsertScheduleSlotAsync(Guid doctorPublicId, UpsertDoctorScheduleSlotRequest request, CancellationToken cancellationToken = default);
    Task DeleteScheduleSlotAsync(Guid doctorPublicId, Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>Расписание с данными брони — для internal-вызовов (календарь врача в Gateway).</summary>
    Task<DoctorScheduleBookingResponseDto> GetScheduleWithBookingsAsync(
        Guid doctorPublicId,
        DateTime? from,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Занимает свободный будущий слот за пациентом. Один UPDATE с условиями — параллельные
    /// брони одного слота не проходят. Возвращает null, если слот уже занят, в прошлом или не найден.
    /// </summary>
    Task<DoctorScheduleSlotBookingDto?> ReserveScheduleSlotAsync(
        Guid doctorPublicId,
        Guid slotId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>Снимает бронь пациента (компенсация, если создать консультацию не удалось).</summary>
    Task<bool> ReleaseScheduleSlotAsync(Guid slotId, Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Привязывает созданную консультацию к брони пациента.</summary>
    Task<bool> LinkScheduleSlotSessionAsync(
        Guid slotId,
        Guid patientId,
        Guid consultationSessionId,
        CancellationToken cancellationToken = default);
    Task<AvailableDoctorDto?> FindAvailableDoctorAsync(string specialty, int urgencyLevel, CancellationToken cancellationToken = default);
    Task EnsureScheduleSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
}
