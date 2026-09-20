using ConsultationService.Application.DTOs;
using ConsultationService.Application.Options;

namespace ConsultationService.Infrastructure.Video;

internal static class VideoRoomMapper
{
    public static VideoRoomResponse ApplyCommon(VideoRoomResponse room, SfuOptions options, string mode, string? role = null)
    {
        room.Mode = mode;
        room.ChatAvailable = true;
        room.SignalingHub = SfuOptions.SignalingHubPath;
        room.IceServers = options.GetIceServers();
        if (!string.IsNullOrWhiteSpace(role))
            room.Role = role;
        return room;
    }
}
