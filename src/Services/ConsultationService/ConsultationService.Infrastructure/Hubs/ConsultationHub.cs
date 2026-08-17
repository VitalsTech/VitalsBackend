using System.Security.Claims;
using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ConsultationService.Infrastructure.Hubs;

[Authorize]
public sealed class ConsultationHub : Hub
{
    private static readonly HashSet<string> AllowedRtcTypes =
        new(StringComparer.OrdinalIgnoreCase) { "offer", "answer", "ice", "hangup", "media" };

    private readonly IConsultationRepository _sessions;

    public ConsultationHub(IConsultationRepository sessions) => _sessions = sessions;

    public async Task JoinSession(string sessionId)
    {
        await EnsureParticipantAsync(sessionId).ConfigureAwait(false);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public Task LeaveSession(string sessionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

    public async Task SendRtcSignal(string sessionId, RtcSignalDto signal)
    {
        await EnsureParticipantAsync(sessionId).ConfigureAwait(false);

        if (signal is null || string.IsNullOrWhiteSpace(signal.Type) || !AllowedRtcTypes.Contains(signal.Type))
            throw new HubException("Unsupported RTC signal type.");

        await Clients.OthersInGroup(sessionId).SendAsync("rtcSignal", new
        {
            sessionId,
            fromUserId = GetUserId(),
            type = signal.Type.Trim().ToLowerInvariant(),
            sdp = signal.Sdp,
            candidate = signal.Candidate,
            sdpMid = signal.SdpMid,
            sdpMLineIndex = signal.SdpMLineIndex,
            audio = signal.Audio,
            video = signal.Video
        });
    }

    private async Task EnsureParticipantAsync(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var id))
            throw new HubException("Invalid session.");

        var ids = GetIdentityIds();
        if (!await _sessions.IsParticipantAsync(id, ids).ConfigureAwait(false))
            throw new HubException("Not a session participant.");
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private IReadOnlyList<Guid> GetIdentityIds()
    {
        var ids = new List<Guid>();
        var userId = GetUserId();
        if (userId != Guid.Empty)
            ids.Add(userId);

        foreach (var claim in Context.User?.FindAll("profile_id") ?? [])
        {
            if (Guid.TryParse(claim.Value, out var profileId) &&
                profileId != Guid.Empty &&
                !ids.Contains(profileId))
            {
                ids.Add(profileId);
            }
        }

        return ids;
    }
}
