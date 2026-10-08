namespace JobsApi.Application.Dtos;

public record LoginDto(string Email, string Password);

public record AuthResultDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string UserId,
    string Email,
    string DisplayName,
    string Role);