using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConsultationService.Infrastructure.Video;

/// <summary>
/// LiveKit SFU integration: creates rooms via RoomService API and issues participant JWT tokens.
/// </summary>
public sealed class LiveKitSfuSignalingService : ISfuSignalingService
{
    private readonly HttpClient _http;
    private readonly SfuOptions _options;
    private readonly ILogger<LiveKitSfuSignalingService> _logger;

    public LiveKitSfuSignalingService(
        HttpClient http,
        IOptions<SfuOptions> options,
        ILogger<LiveKitSfuSignalingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VideoRoomResponse> CreateRoomAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var roomName = $"consultation-{sessionId:N}";
        await EnsureRoomExistsAsync(roomName, cancellationToken).ConfigureAwait(false);
        return VideoRoomMapper.ApplyCommon(new VideoRoomResponse
        {
            RoomId = roomName,
            ServerUrl = _options.ServerUrl,
            AccessToken = string.Empty
        }, _options, "sfu");
    }

    public async Task<VideoRoomResponse> IssueCredentialsAsync(
        Guid sessionId,
        Guid participantId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var roomName = $"consultation-{sessionId:N}";
        await EnsureRoomExistsAsync(roomName, cancellationToken).ConfigureAwait(false);
        var token = CreateParticipantToken(roomName, $"participant-{participantId:N}");
        return VideoRoomMapper.ApplyCommon(new VideoRoomResponse
        {
            RoomId = roomName,
            ServerUrl = _options.ServerUrl,
            AccessToken = token
        }, _options, "sfu", role);
    }

    public async Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiUrl) || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiUrl.TrimEnd('/')}/twirp/livekit.RoomService/DeleteRoom")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { room = roomId }), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateAdminToken());

        try
        {
            await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete LiveKit room {RoomId}", roomId);
        }
    }

    private async Task EnsureRoomExistsAsync(string roomName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiUrl))
            throw new InvalidOperationException("Sfu:ApiUrl is required for LiveKit provider.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiUrl.TrimEnd('/')}/twirp/livekit.RoomService/CreateRoom")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { name = roomName }), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateAdminToken());

        var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"LiveKit CreateRoom failed: {response.StatusCode} {body}");
        }
    }

    private string CreateAdminToken()
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.ApiKey,
            claims: new[] { new Claim("video", JsonSerializer.Serialize(new { roomCreate = true, roomAdmin = true })) },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateParticipantToken(string roomName, string identity)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.ApiKey,
            claims: new[]
            {
                new Claim("sub", identity),
                new Claim("video", JsonSerializer.Serialize(new { roomJoin = true, room = roomName, canPublish = true, canSubscribe = true }))
            },
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
