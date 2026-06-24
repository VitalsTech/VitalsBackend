using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Data;

public static class DoctorScheduleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.DoctorScheduleSlots.AnyAsync().ConfigureAwait(false))
            return;

        var doctors = await db.DoctorProfiles.AsNoTracking().Select(d => d.Id).ToListAsync().ConfigureAwait(false);
        if (doctors.Count == 0)
            return;

        var start = DateTime.UtcNow.Date;
        var slots = new List<Domain.Entities.DoctorScheduleSlot>();

        foreach (var doctorId in doctors)
        {
            for (var day = 0; day < 14; day++)
            {
                var date = start.AddDays(day);
                for (var hour = 9; hour <= 17; hour += 2)
                {
                    slots.Add(new Domain.Entities.DoctorScheduleSlot
                    {
                        DoctorProfileId = doctorId,
                        StartsAt = date.AddHours(hour),
                        EndsAt = date.AddHours(hour + 1),
                        IsAvailable = (day + hour) % 4 != 0,
                        IsOnline = day < 7 || hour <= 15
                    });
                }
            }
        }

        db.DoctorScheduleSlots.AddRange(slots);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
