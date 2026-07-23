using UserService.Application.DTOs.Doctor;

namespace UserService.Application.Interfaces;

public interface IDoctorScheduleService
{
    Task<DoctorSearchResponseDto> SearchDoctorsAsync(DoctorSearchRequest request, CancellationToken cancellationToken = default);
    Task<DoctorScheduleResponseDto> GetScheduleAsync(Guid doctorPublicId, DateTime? from, int days, CancellationToken cancellationToken = default);
    Task<AvailableDoctorDto?> FindAvailableDoctorAsync(string specialty, int urgencyLevel, CancellationToken cancellationToken = default);
    Task EnsureScheduleSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
}
