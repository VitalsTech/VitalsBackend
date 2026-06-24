using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/doctors")]
public sealed class DoctorsController : ControllerBase
{
    [HttpGet]
    public IActionResult NotImplemented() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { error = "Doctor search is not available yet." });
}
