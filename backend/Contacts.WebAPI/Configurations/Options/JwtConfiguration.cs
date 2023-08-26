using System.ComponentModel.DataAnnotations;

namespace Contacts.WebAPI.Configurations.Options;

public class JwtConfiguration
{
    public static readonly string SectionName = "Authentication:Jwt";

    // causes an error if not set in appsettings.json
    [Required]
    [MaxLength(64)]
    public string Issuer { get; set; } = default!;
    [Required]
    [MaxLength(64)]
    public string Audience { get; set; } = default!;
    [Required]
    [MaxLength(256)]
    public string SigningKey { get; set; } = default!;
}