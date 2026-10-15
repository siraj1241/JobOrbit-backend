using JobsApi.Application.Dtos;
using JobsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobsApi.Application.Abstractions;

public interface IResumeService
{
    Task<ResumeDto?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<ResumeDto> UpsertAsync(Guid userId, UpsertResumeDto dto, CancellationToken ct);
    Task<GeneratedBulletsDto> GenerateBulletsAsync(Guid userId, GenerateBulletsRequestDto req, CancellationToken ct);
    Task DeleteAsync(Guid userId, CancellationToken ct);
}
