using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Video;

/// <summary>
/// Generic HTTP SFU adapter for any WebRTC gateway exposing room create/close REST endpoints.
/// </summary>
public sealed class HttpSfuSignalingService : ISfuSignalingService
{
    private readonly HttpClient _http;
    private readonly SfuOptions _options;
    private readonly ILogger<HttpSfuSignalingService> _logger;

    public HttpSfuSignalingService(
        HttpClient http,
        IOptions<SfuOptions> options,
        ILogger<HttpSfuSignalingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VideoRoomResponse> CreateRoomAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.RoomCreateUrl))
            throw new InvalidOperationException("Sfu:RoomCreateUrl is required for Http SFU provider.");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.RoomCreateUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { sessionId }), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.Add("X-Api-Key", _options.ApiKey);

        var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SfuRoomResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SFU provider returned empty room payload.");

        return VideoRoomMapper.ApplyCommon(new VideoRoomResponse
        {
            RoomId = payload.RoomId ?? $"room-{sessionId:N}",
            ServerUrl = payload.ServerUrl ?? _options.ServerUrl,
            AccessToken = payload.AccessToken ?? string.Empty
        }, _options, "sfu");
    }

    public async Task<VideoRoomResponse> IssueCredentialsAsync(
        Guid sessionId,
        Guid participantId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var room = await CreateRoomAsync(sessionId, cancellationToken).ConfigureAwait(false);
        room.Role = role;
        _ = participantId;
        return room;
    }

    public async Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.RoomCloseUrl))
            return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.RoomCloseUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { roomId }), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                request.Headers.Add("X-Api-Key", _options.ApiKey);

            await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close SFU room {RoomId}", roomId);
        }
    }

    private sealed class SfuRoomResponse
    {
        public string? RoomId { get; set; }
        public string? ServerUrl { get; set; }
        public string? AccessToken { get; set; }
    }
}
