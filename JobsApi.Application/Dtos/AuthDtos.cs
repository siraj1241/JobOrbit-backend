namespace JobsApi.Application.Dtos;

public record LoginDto(string Email, string Password);

public record RegisterDto(
    string Email,
    string Password,
    string ConfirmPassword,
    string DisplayName);

public record UpgradeToEmployerDto(string CompanyName);

public record AuthResultDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string UserId,
    string Email,
    string DisplayName,
    string Role);