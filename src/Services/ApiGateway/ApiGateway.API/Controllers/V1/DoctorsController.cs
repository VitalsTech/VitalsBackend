using System.Net.Http.Json;
using ApiGateway.Application.DTOs.Admin;
using ApiGateway.Application.DTOs.Doctors;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/doctors")]
public sealed class DoctorsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public DoctorsController(IBackendForwarder backend) => _backend = backend;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? specialization,
        [FromQuery] string? query,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new UserSearchRequestDto
        {
            UserType = "Doctor",
            Specialization = specialization,
            FirstName = query,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, 100)
        };

        using var response = await _backend.ForwardJsonAsync(
            "user",
            HttpMethod.Post,
            "api/admin/users/search",
            ForwardContext,
            searchRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await ForwardResponse(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<UserSearchBackendResponse>(cancellationToken: cancellationToken)
            ?? new UserSearchBackendResponse();

        var doctors = payload.Items
            .Where(u => u.IsActive && (string.IsNullOrWhiteSpace(u.UserType) || u.UserType.Contains("Doctor", StringComparison.OrdinalIgnoreCase)))
            .Select(u => new DoctorCardDto
            {
                DoctorId = u.PublicId,
                FullName = $"{u.Surename} {u.FirstName} {u.SecondName}".Trim(),
                Specialization = specialization,
                IsActive = u.IsActive
            })
            .ToList();

        var searchResponse = new DoctorSearchResponseDto
        {
            Items = doctors,
            TotalCount = payload.TotalCount,
            PageSize = payload.PageSize
        };
        searchResponse.Page = payload.Page;
        return Ok(searchResponse);
    }

    [HttpGet("{doctorId:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> GetDoctor(Guid doctorId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{doctorId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("{doctorId:guid}/schedule")]
    [AllowAnonymous]
    public Task<IActionResult> GetSchedule(
        Guid doctorId,
        [FromQuery] DateTime? from,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from.HasValue)
            query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        query.Add($"days={Math.Clamp(days, 1, 30)}");
        var path = $"api/doctors/{doctorId}/schedule?{string.Join("&", query)}";
        return Forward(_backend.ForwardAsync("user", HttpMethod.Get, path, ForwardContext, cancellationToken: cancellationToken), cancellationToken);
    }

    private sealed class UserSearchBackendResponse
    {
        public List<UserSearchItem> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class UserSearchItem
    {
        public Guid PublicId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string Surename { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
