using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Infrastructure.Persistence;

namespace LibraryApi.Web.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancers.
/// </summary>
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns application and database health status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> Get(CancellationToken cancellationToken = default)
    {
        var dbOk = true;
        try
        {
            _ = await _db.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbOk = false;
        }
        return Ok(new
        {
            status = "Healthy",
            database = dbOk ? "Ok" : "Unavailable",
            timestamp = DateTime.UtcNow
        });
    }
}
