using ApiGateway.Application.DTOs.Admin;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
public sealed class AdminController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public AdminController(IBackendForwarder backend) => _backend = backend;

    [HttpPost("users/search")]
    public Task<IActionResult> SearchUsers([FromBody] UserSearchRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, "api/admin/users/search", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPut("users/{publicId:guid}/status")]
    public Task<IActionResult> SetStatus(Guid publicId, [FromBody] UserStatusRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Put, $"api/admin/users/{publicId}/status", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("users/{publicId:guid}/block")]
    public Task<IActionResult> BlockUser(Guid publicId, [FromBody] UserBlockRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, $"api/admin/users/{publicId}/block", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("users/{publicId:guid}/unblock")]
    public Task<IActionResult> UnblockUser(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Post, $"api/admin/users/{publicId}/unblock", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpDelete("users/{publicId:guid}/soft")]
    public Task<IActionResult> SoftDelete(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Delete, $"api/admin/users/{publicId}/soft", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("users/{publicId:guid}/restore")]
    public Task<IActionResult> Restore(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Post, $"api/admin/users/{publicId}/restore", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("permissions/check")]
    public Task<IActionResult> CheckPermission([FromBody] PermissionCheckRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, "api/admin/permissions/check", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("permissions/user/{publicId:guid}")]
    public Task<IActionResult> GetUserPermissions(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/admin/permissions/user/{publicId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize]
public sealed class AdminRolesController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public AdminRolesController(IBackendForwarder backend) => _backend = backend;

    [HttpGet]
    public Task<IActionResult> GetRoles(CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, "api/admin/roles", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("permissions")]
    public Task<IActionResult> GetPermissions(CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, "api/admin/roles/permissions", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("users/{publicId:guid}/assign")]
    public Task<IActionResult> AssignRole(Guid publicId, [FromBody] AssignRoleRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, $"api/admin/roles/users/{publicId}/assign", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpDelete("users/{publicId:guid}/unassign")]
    public Task<IActionResult> UnassignRole(Guid publicId, [FromBody] UnassignRoleRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Delete, $"api/admin/roles/users/{publicId}/unassign", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("users/{publicId:guid}")]
    public Task<IActionResult> GetUserRoles(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/admin/roles/users/{publicId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
