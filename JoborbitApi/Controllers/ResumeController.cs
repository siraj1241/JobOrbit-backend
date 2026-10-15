using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoborbitApi.Controllers;

/// <summary>
/// Resume builder endpoints. Authenticated users can create / read / update
/// their own resume, and call the OpenAI-powered bullet generator.
/// </summary>
[ApiController]
[Route("api/resume")]
[Authorize]
public class ResumeController(IResumeService resumes, ILogger<ResumeController> logger) : ControllerBase
{
    /// <summary>Get the current user's resume (if any).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = GetUserId();
        var resume = await resumes.GetByUserIdAsync(userId, ct);
        return resume is null ? NotFound() : Ok(resume);
    }

    /// <summary>Create or replace the current user's resume.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] UpsertResumeDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var resume = await resumes.UpsertAsync(userId, dto, ct);
        return Ok(resume);
    }

    /// <summary>Delete the current user's resume.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var userId = GetUserId();
        await resumes.DeleteAsync(userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Generate 5 AI-tailored resume bullet points + summary suggestion
    /// for a given role (Company + Title). Optionally tailor to a job description.
    /// Requires OpenAI API key to be configured on the server.
    /// </summary>
    [HttpPost("generate-bullets")]
    [ProducesResponseType(typeof(GeneratedBulletsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateBullets([FromBody] GenerateBulletsRequestDto req, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await resumes.GenerateBulletsAsync(userId, req, ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var id))
            throw new UnauthorizedException("Invalid or missing user ID in token.");
        return id;
    }
}
