using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Entities;
using JobsApi.Domain.Exceptions;
using JobsApi.Infrastructure.Options;
using JobsApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JobsApi.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher<User> _hasher;
    private readonly JwtOptions _opts;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        ITokenService tokens,
        IPasswordHasher<User> hasher,
        Microsoft.Extensions.Options.IOptions<JwtOptions> opts,
        ILogger<AuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _hasher = hasher;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(dto.Email, ct);
        if (user is null)
        {
            _logger.LogWarning("Login failed for unknown email: {Email}", dto.Email);
            throw new UnauthorizedException();   // same message as bad password — don't leak
        }

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed (bad password) for {UserId}", user.Id);
            throw new UnauthorizedException();
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        var accessToken = _tokens.GenerateAccessToken(user);
        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new AuthResultDto(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: _opts.AccessMinutes * 60,
            UserId: user.Id.ToString(),
            Email: user.Email,
            DisplayName: user.DisplayName,
            Role: user.Role);
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        if (await _users.ExistsAsync(dto.Email, ct))
        {
            _logger.LogWarning("Registration failed — email already in use: {Email}", dto.Email);
            throw new ConflictException($"An account with email '{dto.Email}' already exists.");
        }

        var user = new User
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            DisplayName = dto.DisplayName.Trim(),
            Role = "User"
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        await _users.AddAsync(user, ct);

        var accessToken = _tokens.GenerateAccessToken(user);
        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        return new AuthResultDto(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: _opts.AccessMinutes * 60,
            UserId: user.Id.ToString(),
            Email: user.Email,
            DisplayName: user.DisplayName,
            Role: user.Role);
    }
}