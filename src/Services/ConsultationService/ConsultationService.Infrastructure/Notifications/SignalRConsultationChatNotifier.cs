using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ConsultationService.Infrastructure.Notifications;

public sealed class SignalRConsultationChatNotifier : IConsultationChatNotifier
{
    private readonly IHubContext<ConsultationHub> _hub;

    public SignalRConsultationChatNotifier(IHubContext<ConsultationHub> hub) => _hub = hub;

    public Task NotifyMessageAsync(Guid sessionId, ConsultationMessageDto message, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(sessionId.ToString()).SendAsync("messageReceived", message, cancellationToken);

    public Task NotifyStatusChangedAsync(Guid sessionId, string status, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(sessionId.ToString()).SendAsync("statusChanged", new { sessionId, status }, cancellationToken);
}
