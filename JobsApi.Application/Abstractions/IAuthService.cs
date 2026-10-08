using JobsApi.Application.Dtos;

namespace JobsApi.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken ct);
    Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct);
}