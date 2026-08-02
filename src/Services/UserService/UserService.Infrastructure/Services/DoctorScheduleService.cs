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

        var start = ToUtc(from ?? DateTime.UtcNow).Date;
        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var end = start.AddDays(Math.Clamp(days, 1, 30));

        var slots = await _db.DoctorScheduleSlots
            .AsNoTracking()
            .Where(s => s.DoctorProfileId == doctorProfile.Id && s.StartsAt >= start && s.StartsAt < end)
            .OrderBy(s => s.StartsAt)
            .Select(s => new DoctorScheduleSlotDto
            {
                Id = s.Id,
                StartsAt = s.StartsAt,
                EndsAt = s.EndsAt,
                IsAvailable = s.IsAvailable,
                IsOnline = s.IsOnline
            })
            .ToListAsync(cancellationToken);

        return new DoctorScheduleResponseDto
        {
            DoctorId = doctorProfile.Profile.User.PublicId,
            Slots = slots
        };
    }

    public async Task<DoctorScheduleBookingResponseDto> GetScheduleWithBookingsAsync(
        Guid doctorPublicId,
        DateTime? from,
        int days,
        CancellationToken cancellationToken = default)
    {
        var doctorProfile = await FindDoctorProfileAsync(doctorPublicId, cancellationToken)
            ?? throw new UserNotFoundException($"Doctor {doctorPublicId} not found.");

        await EnsureScheduleSlotsAsync(doctorProfile.Id, cancellationToken);

        var start = DateTime.SpecifyKind(ToUtc(from ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var end = start.AddDays(Math.Clamp(days, 1, 30));

        var slots = await _db.DoctorScheduleSlots
            .AsNoTracking()
            .Where(s => s.DoctorProfileId == doctorProfile.Id && s.StartsAt >= start && s.StartsAt < end)
            .OrderBy(s => s.StartsAt)
            .Select(s => new DoctorScheduleSlotBookingDto
            {
                Id = s.Id,
                StartsAt = s.StartsAt,
                EndsAt = s.EndsAt,
                IsAvailable = s.IsAvailable,
                IsOnline = s.IsOnline,
                PatientId = s.PatientId,
                ConsultationSessionId = s.ConsultationSessionId,
                BookedAt = s.BookedAt
            })
            .ToListAsync(cancellationToken);

        return new DoctorScheduleBookingResponseDto
        {
            DoctorId = doctorProfile.Profile.User.PublicId,
            Slots = slots
        };
    }

    public async Task<DoctorScheduleSlotBookingDto?> ReserveScheduleSlotAsync(
        Guid doctorPublicId,
        Guid slotId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var doctorProfile = await FindDoctorProfileAsync(doctorPublicId, cancellationToken)
            ?? throw new UserNotFoundException($"Doctor {doctorPublicId} not found.");

        var now = DateTime.UtcNow;

        // Оборванная бронь (есть PatientId, нет сессии) старше 2 минут — освобождаем.
        // Иначе после сбоя шлюза слот навсегда «занят» без консультации.
        await _db.DoctorScheduleSlots
            .Where(s => s.Id == slotId
                && s.DoctorProfileId == doctorProfile.Id
                && s.PatientId != null
                && s.ConsultationSessionId == null
                && s.BookedAt != null
                && s.BookedAt < now.AddMinutes(-2))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.IsAvailable, true)
                    .SetProperty(s => s.PatientId, (Guid?)null)
                    .SetProperty(s => s.BookedAt, (DateTime?)null),
                cancellationToken);

        var affected = await _db.DoctorScheduleSlots
            .Where(s => s.Id == slotId
                && s.DoctorProfileId == doctorProfile.Id
                && s.IsAvailable
                && s.PatientId == null
                && s.StartsAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.IsAvailable, false)
                    .SetProperty(s => s.PatientId, patientId)
                    .SetProperty(s => s.BookedAt, now),
                cancellationToken);

        var slot = await _db.DoctorScheduleSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == slotId && s.DoctorProfileId == doctorProfile.Id, cancellationToken);

        if (slot is null)
            return null;

        // Повтор того же пациента без сессии — продолжаем запись (идемпотентность).
        if (affected == 0
            && slot.PatientId == patientId
            && slot.ConsultationSessionId is null)
        {
            return MapBooking(slot);
        }

        if (affected == 0)
            return null;

        return MapBooking(slot);
    }

    public async Task<bool> ReleaseScheduleSlotAsync(
        Guid slotId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var affected = await _db.DoctorScheduleSlots
            .Where(s => s.Id == slotId && s.PatientId == patientId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.IsAvailable, true)
                    .SetProperty(s => s.PatientId, (Guid?)null)
                    .SetProperty(s => s.ConsultationSessionId, (Guid?)null)
                    .SetProperty(s => s.BookedAt, (DateTime?)null),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> LinkScheduleSlotSessionAsync(
        Guid slotId,
        Guid patientId,
        Guid consultationSessionId,
        CancellationToken cancellationToken = default)
    {
        var affected = await _db.DoctorScheduleSlots
            .Where(s => s.Id == slotId && s.PatientId == patientId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.ConsultationSessionId, consultationSessionId),
                cancellationToken);

        return affected > 0;
    }

    public async Task<DoctorScheduleSlotDto> UpsertScheduleSlotAsync(
        Guid doctorPublicId,
        UpsertDoctorScheduleSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var doctorProfile = await _db.DoctorProfiles
            .Include(d => d.Profile)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(d =>
                d.Profile.ProfileType == ProfileType.Doctor &&
                (d.Profile.User.PublicId == doctorPublicId || d.Id == doctorPublicId || d.Profile.Id == doctorPublicId),
                cancellationToken)
            ?? throw new UserNotFoundException($"Doctor {doctorPublicId} not found.");

        var startsAt = ToUtc(request.StartsAt);
        var endsAt = ToUtc(request.EndsAt);

        if (endsAt <= startsAt)
            throw new InvalidOperationException("Время окончания должно быть позже начала.");

        if (startsAt < DateTime.UtcNow.AddMinutes(-5))
            throw new InvalidOperationException("Нельзя создавать слот в прошлом.");

        DoctorScheduleSlot slot;
        if (request.Id.HasValue)
        {
            slot = await _db.DoctorScheduleSlots
                .FirstOrDefaultAsync(s => s.Id == request.Id.Value && s.DoctorProfileId == doctorProfile.Id, cancellationToken)
                ?? throw new KeyNotFoundException("Слот расписания не найден.");

            if (slot.PatientId is not null)
                throw new InvalidOperationException("Слот забронирован пациентом, изменить его нельзя.");

            slot.StartsAt = startsAt;
            slot.EndsAt = endsAt;
            slot.IsAvailable = request.IsAvailable;
            slot.IsOnline = request.IsOnline;
        }
        else
        {
            slot = new DoctorScheduleSlot
            {
                DoctorProfileId = doctorProfile.Id,
                StartsAt = startsAt,
                EndsAt = endsAt,
                IsAvailable = request.IsAvailable,
                IsOnline = request.IsOnline
            };
            _db.DoctorScheduleSlots.Add(slot);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new DoctorScheduleSlotDto
        {
            Id = slot.Id,
            StartsAt = slot.StartsAt,
            EndsAt = slot.EndsAt,
            IsAvailable = slot.IsAvailable,
            IsOnline = slot.IsOnline
        };
    }

    public async Task DeleteScheduleSlotAsync(
        Guid doctorPublicId,
        Guid slotId,
        CancellationToken cancellationToken = default)
    {
        var doctorProfile = await FindDoctorProfileAsync(doctorPublicId, cancellationToken)
            ?? throw new UserNotFoundException($"Doctor {doctorPublicId} not found.");

        var slot = await _db.DoctorScheduleSlots
            .FirstOrDefaultAsync(s => s.Id == slotId && s.DoctorProfileId == doctorProfile.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Слот расписания не найден.");

        if (slot.PatientId is not null)
            throw new InvalidOperationException("Слот забронирован пациентом, удалить его нельзя.");

        _db.DoctorScheduleSlots.Remove(slot);
        await _db.SaveChangesAsync(cancellationToken);
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

        var aliases = ExpandSpecialtyAliases(normalizedSpecialty);
        var doctors = await _db.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.Profile)
            .ThenInclude(p => p.User)
            .Where(d => d.Profile.IsActive && d.Profile.ProfileType == ProfileType.Doctor)
            .ToListAsync(cancellationToken);

        doctors = doctors
            .Where(d => aliases.Any(a => d.Specialization.Contains(a, StringComparison.OrdinalIgnoreCase)))
            .ToList();

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
            // Нет слота в ближайшие 8ч — всё равно назначаем врача (чат/запись), иначе triage→route ломается ночью.
            var load = todayLoads.GetValueOrDefault(doctor.Id, 0);
            var seniorBoost = urgencyLevel >= 4 && doctor.Category is DoctorCategory.First or DoctorCategory.Highest ? -1 : 0;
            var slotPenalty = hasOnlineSlot ? 0 : 50;
            var score = load + seniorBoost + slotPenalty;
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = new AvailableDoctorDto
            {
                DoctorId = doctor.Profile.User.PublicId,
                FullName = $"{doctor.Profile.User.Surename} {doctor.Profile.User.FirstName} {doctor.Profile.User.SecondName}".Trim(),
                Specialty = doctor.Specialization,
                IsOnline = hasOnlineSlot,
                TodayLoad = load,
                IsSenior = doctor.Category is DoctorCategory.First or DoctorCategory.Highest
            };
        }

        return best;
    }

    private static IReadOnlyList<string> ExpandSpecialtyAliases(string specialty)
    {
        var list = new List<string> { specialty };
        switch (specialty)
        {
            case "therapist":
            case "терапевт":
                list.AddRange(["therapist", "терапевт", "therapy"]);
                break;
            case "pediatrician":
            case "педиатр":
                list.AddRange(["pediatrician", "педиатр", "pediatrics"]);
                break;
            case "cardiologist":
            case "кардиолог":
                list.AddRange(["cardiologist", "кардиолог"]);
                break;
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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

    private static DoctorScheduleSlotBookingDto MapBooking(DoctorScheduleSlot slot) => new()
    {
        Id = slot.Id,
        StartsAt = slot.StartsAt,
        EndsAt = slot.EndsAt,
        IsAvailable = slot.IsAvailable,
        IsOnline = slot.IsOnline,
        PatientId = slot.PatientId,
        ConsultationSessionId = slot.ConsultationSessionId,
        BookedAt = slot.BookedAt
    };

    /// <summary>Столбцы слотов — timestamptz, Npgsql отклоняет Unspecified из query/body.</summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

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
