using System.ComponentModel.DataAnnotations;

namespace JobsApi.Infrastructure.Options;

public class JwtOptions
{
    public const string Section = "Jwt";

    [Required]
    public string Secret { get; set; } = default!;

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Range(1, 1440)]
    public int AccessMinutes { get; set; } = 15;
}