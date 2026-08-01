using ApiGateway.Application.DTOs.Common;
using ApiGateway.Application.DTOs.Doctors;

namespace ApiGateway.Application.Interfaces;

public interface IDoctorCalendarService
{
    /// <summary>
    /// Собирает календарь врача из расписания (UserService), консультаций (ConsultationService),
    /// результатов триажа (AITriageService) и анамнеза (MedicalRecordService).
    /// </summary>
    Task<DoctorCalendarResponseDto> GetCalendarAsync(
        Guid doctorId,
        IReadOnlyList<Guid> doctorIdentityIds,
        DateTime? from,
        int days,
        BackendForwardContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Запись пациента на слот: резерв слота в UserService, затем консультация на его время.
    /// Если консультацию создать не удалось, бронь снимается.
    /// </summary>
    Task<BookConsultationResult> BookAsync(
        Guid patientId,
        BookConsultationRequestDto request,
        BackendForwardContext context,
        CancellationToken cancellationToken = default);
}
