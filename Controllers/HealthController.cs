using Microsoft.AspNetCore.Mvc;

namespace QwenTtsOpenAIProxy.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "QwenTtsOpenAIProxy",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
