using Microsoft.EntityFrameworkCore;
using UserService.Application.DTOs.Doctor;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Services;

public sealed class DoctorScheduleService : IDoctorScheduleService
{
    private readonly AppDbContext _db;

    public DoctorScheduleService(AppDbContext db) => _db = db;

    public async Task<DoctorScheduleResponseDto> GetScheduleAsync(
        Guid doctorPublicId,
        DateTime? from,
        int days,
        CancellationToken cancellationToken = default)
    {
        var doctorProfile = await FindDoctorProfileAsync(doctorPublicId, cancellationToken)
            ?? throw new KeyNotFoundException($"Doctor {doctorPublicId} not found.");

        var start = (from ?? DateTime.UtcNow).Date;
        var end = start.AddDays(Math.Clamp(days, 1, 30));

        var slots = await _db.DoctorScheduleSlots
            .AsNoTracking()
            .Where(s => s.DoctorProfileId == doctorProfile.Id && s.StartsAt >= start && s.StartsAt < end)
            .OrderBy(s => s.StartsAt)
            .Select(s => new DoctorScheduleSlotDto
            {
                StartsAt = s.StartsAt,
                EndsAt = s.EndsAt,
                IsAvailable = s.IsAvailable
            })
            .ToListAsync(cancellationToken);

        return new DoctorScheduleResponseDto
        {
            DoctorId = doctorPublicId,
            Slots = slots
        };
    }

    public async Task<AvailableDoctorDto?> FindAvailableDoctorAsync(
        string specialty,
        int urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var horizon = now.AddHours(8);
        var normalizedSpecialty = specialty.Trim().ToLowerInvariant();

        var doctors = await _db.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.Profile)
            .ThenInclude(p => p.User)
            .Where(d => d.Profile.IsActive && d.Profile.ProfileType == ProfileType.Doctor)
            .Where(d => d.Specialization.ToLower() == normalizedSpecialty)
            .ToListAsync(cancellationToken);

        if (doctors.Count == 0)
            return null;

        var doctorIds = doctors.Select(d => d.Id).ToList();
        var upcomingSlots = await _db.DoctorScheduleSlots
            .AsNoTracking()
            .Where(s => doctorIds.Contains(s.DoctorProfileId) && s.StartsAt >= now && s.StartsAt < horizon)
            .ToListAsync(cancellationToken);

        var todayLoads = await _db.DoctorScheduleSlots
            .AsNoTracking()
            .Where(s => doctorIds.Contains(s.DoctorProfileId) && s.StartsAt >= todayStart && s.StartsAt < todayEnd && !s.IsAvailable)
            .GroupBy(s => s.DoctorProfileId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        AvailableDoctorDto? best = null;
        var bestScore = int.MaxValue;

        foreach (var doctor in doctors)
        {
            var hasOnlineSlot = upcomingSlots.Any(s =>
                s.DoctorProfileId == doctor.Id && s.IsAvailable && s.IsOnline);
            if (!hasOnlineSlot)
                continue;

            var load = todayLoads.GetValueOrDefault(doctor.Id, 0);
            var seniorBoost = urgencyLevel >= 4 && doctor.Category is DoctorCategory.First or DoctorCategory.Highest ? -1 : 0;
            var score = load + seniorBoost;
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = new AvailableDoctorDto
            {
                DoctorId = doctor.Profile.User.PublicId,
                FullName = $"{doctor.Profile.User.Surename} {doctor.Profile.User.FirstName} {doctor.Profile.User.SecondName}".Trim(),
                Specialty = doctor.Specialization,
                IsOnline = true,
                TodayLoad = load,
                IsSenior = doctor.Category is DoctorCategory.First or DoctorCategory.Highest
            };
        }

        return best;
    }

    private async Task<DoctorProfile?> FindDoctorProfileAsync(Guid doctorPublicId, CancellationToken cancellationToken) =>
        await _db.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.Profile)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(d => d.Profile.User.PublicId == doctorPublicId && d.Profile.ProfileType == ProfileType.Doctor, cancellationToken);
}
