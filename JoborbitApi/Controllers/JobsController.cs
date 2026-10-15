using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoborbitApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(IJobService jobs, ILogger<JobsController> logger) : ControllerBase
{
    /// <summary>Fetch jobs live from the external provider (Arbeitnow) — not persisted.</summary>
    [HttpGet("external")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResultDto<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetExternal(
        [FromQuery] string? search,
        [FromQuery] string? location,
        [FromQuery] bool? remote,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new ExternalJobsQueryDto(search, location, remote, page, pageSize);
        var result = await jobs.GetExternalAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Search locally cached jobs (with pagination & filters).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResultDto<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? location,
        [FromQuery] bool? remote,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new JobsQueryDto(search, location, remote, tag, page, pageSize);
        var result = await jobs.SearchAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Get a single cached job by ID.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(id, ct);
        return job is null ? NotFound() : Ok(job);
    }

    /// <summary>Pull the latest jobs from the external provider and upsert into our DB.
    /// Requires the caller to be authenticated (Admin role recommended).</summary>
    [HttpPost("sync")]
    [Authorize(Roles = "Admin,User")]
    [ProducesResponseType(typeof(SyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sync(
        [FromQuery] int maxPages = 3,
        CancellationToken ct = default)
    {
        maxPages = Math.Clamp(maxPages, 1, 10);
        logger.LogInformation("Starting external job sync (maxPages={MaxPages}).", maxPages);
        var result = await jobs.SyncFromExternalAsync(maxPages, ct);
        logger.LogInformation(
            "Job sync complete: fetched={Fetched}, added={Added}, updated={Updated}, skipped={Skipped}.",
            result.Fetched, result.Added, result.Updated, result.Skipped);
        return Ok(result);
    }
}
