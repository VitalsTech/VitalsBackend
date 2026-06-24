using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Hosted;

public sealed class SessionTimeoutHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ConsultationOptions _options;
    private readonly ILogger<SessionTimeoutHostedService> _logger;

    public SessionTimeoutHostedService(
        IServiceProvider services,
        IOptions<ConsultationOptions> options,
        ILogger<SessionTimeoutHostedService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IConsultationRepository>();
                var consultation = scope.ServiceProvider.GetRequiredService<IConsultationService>();
                var expired = await repo.GetExpiredCandidatesAsync(DateTime.UtcNow, stoppingToken);

                foreach (var session in expired)
                {
                    _logger.LogInformation("Expiring session {SessionId}", session.Id);
                    await consultation.ExpireAsync(session.Id, "No participant joined within timeout", stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Session timeout scan failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
