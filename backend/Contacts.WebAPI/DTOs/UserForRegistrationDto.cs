using System.ComponentModel.DataAnnotations;

namespace Contacts.WebAPI.DTOs;

public class UserForRegistrationDto
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MaxLength(64)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [MinLength(1)]
    [MaxLength(16)]
    public ICollection<string> Roles { get; set; } = new List<string>();
}