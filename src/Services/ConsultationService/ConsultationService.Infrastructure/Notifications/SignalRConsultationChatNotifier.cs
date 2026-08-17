using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Domain.Enums;
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

    public Task NotifyMessagesReadAsync(
        Guid sessionId,
        ParticipantRole readerRole,
        DateTime readAt,
        long lastSequence,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(sessionId.ToString()).SendAsync(
            "messagesRead",
            new { sessionId, readerRole = readerRole.ToString(), readAt, lastSequence },
            cancellationToken);

    public Task NotifyVideoStartedAsync(Guid sessionId, VideoRoomResponse room, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(sessionId.ToString()).SendAsync(
            "videoStarted",
            new
            {
                sessionId,
                mode = room.Mode,
                roomId = room.RoomId,
                signalingHub = room.SignalingHub,
                chatAvailable = room.ChatAvailable,
                iceServers = room.IceServers
            },
            cancellationToken);

    public Task NotifyVideoStoppedAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(sessionId.ToString()).SendAsync(
            "videoStopped",
            new { sessionId },
            cancellationToken);

    public Task NotifyClinicalActionAsync(Guid sessionId, ClinicalActionDto action, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(sessionId.ToString()).SendAsync("clinicalAction", action, cancellationToken);
}
