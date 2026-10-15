using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoborbitApi.Controllers;

/// <summary>
/// Employer-only endpoints for posting &amp; managing internal jobs.
/// All routes require the caller's JWT to carry the "Employer" role.
/// </summary>
[ApiController]
[Route("api/employer/jobs")]
[Authorize(Roles = "Employer")]
public class EmployerJobsController(IInternalJobService jobs, ILogger<EmployerJobsController> logger) : ControllerBase
{
    /// <summary>List the current employer's job postings (all statuses).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobPostingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMine(CancellationToken ct)
    {
        var employerId = GetEmployerId();
        var list = await jobs.ListByEmployerAsync(employerId, ct);
        return Ok(list);
    }

    /// <summary>Get a single posting owned by the current employer.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var employerId = GetEmployerId();
        var job = await jobs.GetByIdForEmployerAsync(employerId, id, ct);
        return job is null ? NotFound() : Ok(job);
    }

    /// <summary>Create a new job posting (Draft or Published).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateJobPostingDto dto, CancellationToken ct)
    {
        var employerId = GetEmployerId();
        var job = await jobs.CreateAsync(employerId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    /// <summary>Update an existing posting.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateJobPostingDto dto, CancellationToken ct)
    {
        var employerId = GetEmployerId();
        var job = await jobs.UpdateAsync(employerId, id, dto, ct);
        return Ok(job);
    }

    /// <summary>Delete a posting.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var employerId = GetEmployerId();
        await jobs.DeleteAsync(employerId, id, ct);
        return NoContent();
    }

    private Guid GetEmployerId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var id))
            throw new UnauthorizedException("Invalid or missing user ID in token.");
        return id;
    }
}
