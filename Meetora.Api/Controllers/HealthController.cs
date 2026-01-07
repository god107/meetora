using Meetora.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meetora.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HealthController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("db")]
    public async Task<IActionResult> GetDatabaseHealth(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return Ok(new { dbStatus = "ok" });
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { dbStatus = "down" });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { dbStatus = "down" });
        }
    }
}
