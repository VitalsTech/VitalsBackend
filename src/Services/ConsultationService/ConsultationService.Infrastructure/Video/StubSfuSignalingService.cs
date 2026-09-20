using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Video;

/// <summary>
/// DEV P2P WebRTC: комната — идентификатор сессии, ICE — публичный STUN, сигналинг через SignalR.
/// </summary>
public sealed class StubSfuSignalingService : ISfuSignalingService
{
    private readonly SfuOptions _options;

    public StubSfuSignalingService(IOptions<SfuOptions> options) => _options = options.Value;

    public Task<VideoRoomResponse> CreateRoomAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Build(sessionId, role: null));

    public Task<VideoRoomResponse> IssueCredentialsAsync(
        Guid sessionId,
        Guid participantId,
        string role,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Build(sessionId, role, participantId));

    public Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private VideoRoomResponse Build(Guid sessionId, string? role, Guid? participantId = null)
    {
        var roomId = $"room-{sessionId:N}";
        var tokenSeed = participantId ?? sessionId;
        return VideoRoomMapper.ApplyCommon(new VideoRoomResponse
        {
            RoomId = roomId,
            ServerUrl = string.Empty,
            AccessToken = Convert.ToBase64String(tokenSeed.ToByteArray())
        }, _options, "p2p", role);
    }
}
