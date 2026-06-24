using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/consultations")]
public sealed class ConsultationsController : ControllerBase
{
    [HttpGet]
    public IActionResult NotImplemented() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { error = "Consultation Service is not connected yet." });
}

[ApiController]
[Route("api/v1/doctors")]
public sealed class DoctorsController : ControllerBase
{
    [HttpGet]
    public IActionResult NotImplemented() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { error = "Doctor search is not available yet." });
}

[ApiController]
[Route("api/v1/prescriptions")]
public sealed class PrescriptionsController : ControllerBase
{
    [HttpGet]
    public IActionResult NotImplemented() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { error = "Prescription Service is not connected yet." });
}
