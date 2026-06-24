using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.State;

public sealed class InMemorySessionStateStore : ISessionStateStore
{
    private readonly ILogger<InMemorySessionStateStore> _logger;
    private readonly RedisOptions _redis;

    public InMemorySessionStateStore(ILogger<InMemorySessionStateStore> logger, IOptions<RedisOptions> redis)
    {
        _logger = logger;
        _redis = redis.Value;
    }

    public Task SetSessionStateAsync(
        Guid sessionId,
        string status,
        DateTime lastActivity,
        int patientUnread,
        int doctorUnread,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Session state {SessionId}: status={Status} lastActivity={LastActivity} patientUnread={PatientUnread} doctorUnread={DoctorUnread} redisEnabled={RedisEnabled}",
            sessionId,
            status,
            lastActivity,
            patientUnread,
            doctorUnread,
            _redis.Enabled);

        return Task.CompletedTask;
    }
}
