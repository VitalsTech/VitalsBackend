using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Data;

public static class DoctorScheduleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var schedule = scope.ServiceProvider.GetRequiredService<IDoctorScheduleService>();

        var doctorIds = await db.DoctorProfiles.AsNoTracking().Select(d => d.Id).ToListAsync().ConfigureAwait(false);
        foreach (var doctorId in doctorIds)
            await schedule.EnsureScheduleSlotsAsync(doctorId).ConfigureAwait(false);
    }
}
