using Microsoft.EntityFrameworkCore;
using UserService.Application.DTOs.Doctor;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Services;

public sealed class DoctorScheduleService : IDoctorScheduleService
{
    private readonly AppDbContext _db;

    public DoctorScheduleService(AppDbContext db) => _db = db;

    public async Task<DoctorSearchResponseDto> SearchDoctorsAsync(
        DoctorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(0, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 100);

        var query = _db.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.Profile)
            .ThenInclude(p => p.User)
            .Where(d => d.Profile.ProfileType == ProfileType.Doctor && d.Profile.User.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Specialization))
        {
            var specialization = request.Specialization.Trim().ToLower();
            query = query.Where(d => d.Specialization.ToLower().Contains(specialization));
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var terms = request.Query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLower())
                .ToArray();

            foreach (var term in terms)
            {
                query = query.Where(d =>
                    d.Profile.User.FirstName.ToLower().Contains(term) ||
                    (d.Profile.User.SecondName != null && d.Profile.User.SecondName.ToLower().Contains(term)) ||
                    d.Profile.User.Surename.ToLower().Contains(term));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var doctors = await query
            .OrderBy(d => d.Profile.User.Surename)
            .ThenBy(d => d.Profile.User.FirstName)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = doctors.Select(d => new DoctorCardDto
        {
            DoctorId = d.Profile.User.PublicId,
            FullName = $"{d.Profile.User.Surename} {d.Profile.User.FirstName} {d.Profile.User.SecondName}".Trim(),
            Specialization = d.Specialization,
            Rating = d.Rating,
            Biography = d.Biography,
            IsActive = d.Profile.IsActive && d.Profile.User.IsActive
        }).ToList();

        return new DoctorSearchResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DoctorScheduleResponseDto> GetScheduleAsync(
        Guid doctorPublicId,
        DateTime? from,
        int days,
        CancellationToken cancellationToken = default)
    {
        var doctorProfile = await FindDoctorProfileAsync(doctorPublicId, cancellationToken)
            ?? throw new UserNotFoundException($"Doctor {doctorPublicId} not found.");

        await EnsureScheduleSlotsAsync(doctorProfile.Id, cancellationToken);

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
            DoctorId = doctorProfile.Profile.User.PublicId,
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

        foreach (var doctor in doctors)
            await EnsureScheduleSlotsAsync(doctor.Id, cancellationToken);

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

    public async Task EnsureScheduleSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default)
    {
        var horizonStart = DateTime.UtcNow.Date;
        var horizonEnd = horizonStart.AddDays(14);
        var hasUpcoming = await _db.DoctorScheduleSlots
            .AnyAsync(s => s.DoctorProfileId == doctorProfileId && s.StartsAt >= horizonStart && s.StartsAt < horizonEnd, cancellationToken);
        if (hasUpcoming)
            return;

        var slots = BuildSlots(doctorProfileId, horizonStart, days: 14);
        _db.DoctorScheduleSlots.AddRange(slots);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<DoctorProfile?> FindDoctorProfileAsync(Guid doctorId, CancellationToken cancellationToken) =>
        await _db.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.Profile)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(d =>
                d.Profile.ProfileType == ProfileType.Doctor &&
                (d.Profile.User.PublicId == doctorId || d.Id == doctorId || d.Profile.Id == doctorId),
                cancellationToken);

    internal static List<DoctorScheduleSlot> BuildSlots(Guid doctorProfileId, DateTime startDate, int days)
    {
        var slots = new List<DoctorScheduleSlot>();
        for (var day = 0; day < days; day++)
        {
            var date = startDate.AddDays(day);
            for (var hour = 9; hour <= 17; hour += 2)
            {
                slots.Add(new DoctorScheduleSlot
                {
                    DoctorProfileId = doctorProfileId,
                    StartsAt = DateTime.SpecifyKind(date.AddHours(hour), DateTimeKind.Utc),
                    EndsAt = DateTime.SpecifyKind(date.AddHours(hour + 1), DateTimeKind.Utc),
                    IsAvailable = (day + hour) % 4 != 0,
                    IsOnline = day < 7 || hour <= 15
                });
            }
        }

        return slots;
    }
}
