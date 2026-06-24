using RoutingService.Domain.Entities;
using RoutingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RoutingService.Infrastructure.Data;

public static class RoutingDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoutingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RoutingDbContext>>();

        if (await db.ClinicRoutingRules.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        db.ClinicRoutingRules.AddRange(
            new ClinicRoutingRule
            {
                Id = Guid.NewGuid(),
                ClinicId = "default",
                RuleKey = "force_therapist_for_fever",
                RuleValue = "38.5",
                Description = "Patients with fever above threshold route to therapist first",
                IsActive = true,
                UpdatedAt = now
            },
            new ClinicRoutingRule
            {
                Id = Guid.NewGuid(),
                ClinicId = "default",
                RuleKey = "dms_priority_boost",
                RuleValue = "true",
                Description = "Boost queue priority for DMS patients",
                IsActive = true,
                UpdatedAt = now
            });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default clinic routing rules");
    }
}
