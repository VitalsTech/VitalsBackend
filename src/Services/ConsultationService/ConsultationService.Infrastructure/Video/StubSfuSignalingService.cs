using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Video;

public sealed class StubSfuSignalingService : ISfuSignalingService
{
    private readonly SfuOptions _options;

    public StubSfuSignalingService(IOptions<SfuOptions> options) => _options = options.Value;

    public Task<VideoRoomResponse> CreateRoomAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var roomId = $"room-{sessionId:N}";
        return Task.FromResult(new VideoRoomResponse
        {
            RoomId = roomId,
            ServerUrl = _options.ServerUrl,
            AccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        });
    }

    public Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
