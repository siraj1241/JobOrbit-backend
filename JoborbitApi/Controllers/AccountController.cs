using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoborbitApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAuthService auth, ILogger<AccountController> logger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        => Ok(await auth.LoginAsync(dto, ct));

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(dto, ct);
        return CreatedAtAction(
            nameof(Me),
            new { },
            result);
    }

    /// <summary>Upgrade the currently-authenticated user to Employer role.</summary>
    [HttpPost("upgrade-to-employer")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpgradeToEmployer(
        [FromBody] UpgradeToEmployerDto dto, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedException("User ID not found in token.");
        var result = await auth.UpgradeToEmployerAsync(Guid.Parse(userId), dto.CompanyName, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        UserId = User.FindFirst("sub")?.Value,
        Email = User.FindFirst("email")?.Value,
        Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
}