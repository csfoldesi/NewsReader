using Microsoft.AspNetCore.Mvc;

namespace NewsReader.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    public ActionResult<PingResponse> Get()
    {
        return Ok(new PingResponse("pong", DateTimeOffset.UtcNow));
    }
}

public record PingResponse(string Message, DateTimeOffset UtcTime);