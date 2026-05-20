using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;
using IzolluVakfi.Data.Enums;

namespace IzolluVakfi.Services;

public class RbacService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RbacService> _logger;

    public RbacService(ApplicationDbContext context, ILogger<RbacService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AppUser?> ValidateUserAsync(string username, string password)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null) return null;

        if (HashPassword(password, user.PasswordSalt) != user.PasswordHash) return null;

        user.LastLoginAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<AppUser>> GetAllUsersAsync()
    {
        return await _context.AppUsers
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<AppUser?> GetUserByIdAsync(int id)
    {
        return await _context.AppUsers.FindAsync(id);
    }

    public async Task<(bool Success, string Error)> CreateUserAsync(
        string username, string password, UserRole role, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Şifre en az 6 karakter olmalıdır.");

        if (await _context.AppUsers.AnyAsync(u => u.Username == username))
            return (false, "Bu kullanıcı adı zaten kullanılıyor.");

        var salt = GenerateSalt();
        var user = new AppUser
        {
            Username = username,
            PasswordHash = HashPassword(password, salt),
            PasswordSalt = salt,
            Role = role,
            DisplayName = displayName ?? username,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User created: {Username}, Role: {Role}", username, role);
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateUserAsync(
        int id, UserRole role, string? displayName, bool isActive)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null) return (false, "Kullanıcı bulunamadı.");

        if (user.Role == UserRole.SuperAdmin && role != UserRole.SuperAdmin)
        {
            var superAdminCount = await _context.AppUsers
                .CountAsync(u => u.Role == UserRole.SuperAdmin && u.IsActive && u.Id != id);
            if (superAdminCount == 0)
                return (false, "Son super admin kullanıcısının rolü değiştirilemez.");
        }

        user.Role = role;
        user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? user.Username : displayName;
        user.IsActive = isActive;
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> ChangePasswordAsync(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Şifre en az 6 karakter olmalıdır.");

        var user = await _context.AppUsers.FindAsync(id);
        if (user == null) return (false, "Kullanıcı bulunamadı.");

        user.PasswordSalt = GenerateSalt();
        user.PasswordHash = HashPassword(newPassword, user.PasswordSalt);
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteUserAsync(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null) return (false, "Kullanıcı bulunamadı.");

        if (user.Role == UserRole.SuperAdmin)
        {
            var superAdminCount = await _context.AppUsers
                .CountAsync(u => u.Role == UserRole.SuperAdmin && u.IsActive);
            if (superAdminCount <= 1)
                return (false, "Son super admin kullanıcısı silinemez.");
        }

        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task SeedDefaultAdminAsync()
    {
        if (await _context.AppUsers.AnyAsync()) return;

        // Şifre formatı: {username}44@@**
        static string Pw(string username) => $"{username}44@@**";

        var usersToSeed = new[]
        {
            // SuperAdmin
            new { Username = "superadmin", DisplayName = "Super Admin",          Role = UserRole.SuperAdmin },

            // Admin — okuma + yazma + silme + import + ayarlar
            new { Username = "izollu",     DisplayName = "İzollu",               Role = UserRole.Admin },
            new { Username = "operator",   DisplayName = "Operatör",             Role = UserRole.Admin },

            // ReadOnly — sadece okuma + export
            new { Username = "adnanb",     DisplayName = "Adnan Başdemir",       Role = UserRole.ReadOnly },
            new { Username = "vedatt",     DisplayName = "Vedat Toy",            Role = UserRole.ReadOnly },
            new { Username = "uye",        DisplayName = "Üye",                  Role = UserRole.ReadOnly },
            new { Username = "baskan",     DisplayName = "Başkan",               Role = UserRole.ReadOnly },
            new { Username = "sekreter",   DisplayName = "Sekreter",             Role = UserRole.ReadOnly },
            new { Username = "abdullahk",  DisplayName = "Abdullah Kayaduman",   Role = UserRole.ReadOnly },
            new { Username = "yasara",     DisplayName = "Yaşar Altunbey",       Role = UserRole.ReadOnly },
            new { Username = "ihsane",     DisplayName = "İhsan Ekici",          Role = UserRole.ReadOnly },
            new { Username = "nazifo",     DisplayName = "Nazif Özbek",          Role = UserRole.ReadOnly },
            new { Username = "burako",     DisplayName = "Oğuz Burak Özbek",     Role = UserRole.ReadOnly },
            new { Username = "fatihs",     DisplayName = "Fatih Şişman",         Role = UserRole.ReadOnly },
            new { Username = "hacig",      DisplayName = "Hacı Bayram Gökhan",   Role = UserRole.ReadOnly },
            new { Username = "bahara",     DisplayName = "Bahar Altunbay",       Role = UserRole.ReadOnly },
        };

        foreach (var u in usersToSeed)
        {
            var salt = GenerateSalt();
            _context.AppUsers.Add(new AppUser
            {
                Username = u.Username,
                PasswordHash = HashPassword(Pw(u.Username), salt),
                PasswordSalt = salt,
                Role = u.Role,
                DisplayName = u.DisplayName,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default users", usersToSeed.Length);
    }

    private static string HashPassword(string password, string salt)
    {
        var combined = Encoding.UTF8.GetBytes(password + salt);
        return Convert.ToBase64String(SHA256.HashData(combined));
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
