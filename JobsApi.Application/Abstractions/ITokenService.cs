

using JobsApi.Domain.Entities;

namespace JobsApi.Application.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}