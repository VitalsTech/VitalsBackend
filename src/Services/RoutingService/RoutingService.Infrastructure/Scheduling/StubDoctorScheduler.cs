using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;

namespace RoutingService.Infrastructure.Scheduling;

public sealed class StubDoctorScheduler : IDoctorScheduler
{
    private static readonly IReadOnlyList<DoctorSlotDto> Doctors =
    [
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"), FullName = "Иванова И.И.", Specialty = "therapist", IsOnline = true, TodayLoad = 3, IsSenior = true },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"), FullName = "Петров А.С.", Specialty = "therapist", IsOnline = true, TodayLoad = 5, IsSenior = false },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111103"), FullName = "Сидорова М.П.", Specialty = "pediatrician", IsOnline = true, TodayLoad = 2, IsSenior = true },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111104"), FullName = "Козлов В.Е.", Specialty = "cardiologist", IsOnline = false, TodayLoad = 8, IsSenior = true },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111105"), FullName = "Морозова Е.Д.", Specialty = "cardiologist", IsOnline = true, TodayLoad = 4, IsSenior = false },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111106"), FullName = "Волкова О.Н.", Specialty = "endocrinologist", IsOnline = true, TodayLoad = 1, IsSenior = true },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111107"), FullName = "Лебедев К.Р.", Specialty = "allergist", IsOnline = true, TodayLoad = 2, IsSenior = false },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111108"), FullName = "Новикова Т.А.", Specialty = "neurologist", IsOnline = true, TodayLoad = 6, IsSenior = true },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111109"), FullName = "Федоров Д.М.", Specialty = "gastroenterologist", IsOnline = true, TodayLoad = 3, IsSenior = false },
        new() { DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111110"), FullName = "Смирнова Л.В.", Specialty = "psychiatrist", IsOnline = true, TodayLoad = 2, IsSenior = true }
    ];

    public Task<DoctorSlotDto?> FindAvailableDoctorAsync(string specialty, int urgencyLevel, CancellationToken cancellationToken = default)
    {
        var candidates = Doctors
            .Where(d => d.Specialty.Equals(specialty, StringComparison.OrdinalIgnoreCase) && d.IsOnline)
            .OrderBy(d => d.TodayLoad)
            .ThenByDescending(d => urgencyLevel >= 4 && d.IsSenior)
            .ToList();

        return Task.FromResult(candidates.FirstOrDefault());
    }
}
