using System.ComponentModel.DataAnnotations;
using IzolluVakfi.Data.Enums;

namespace IzolluVakfi.Data.Entities;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string PasswordSalt { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.ReadOnly;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLoginAt { get; set; }

    [StringLength(200)]
    public string? DisplayName { get; set; }
}
