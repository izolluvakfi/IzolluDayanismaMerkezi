using IzolluVakfi.Data.Entities;
using IzolluVakfi.Data.Enums;

namespace IzolluVakfi.Services;

public static class Permissions
{
    public const string Read = "read";
    public const string Export = "export";
    public const string Write = "write";
    public const string Delete = "delete";
    public const string Import = "import";
    public const string Settings = "settings";
    public const string UserManagement = "user_management";
}

public class AuthService
{
    public bool IsAuthenticated { get; private set; }
    public int UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.ReadOnly;

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool CanWrite => Role == UserRole.SuperAdmin || Role == UserRole.Admin;
    public bool CanDelete => Role == UserRole.SuperAdmin || Role == UserRole.Admin;

    public event Action? OnAuthStateChanged;

    public void SetUser(AppUser user)
    {
        IsAuthenticated = true;
        UserId = user.Id;
        Username = user.Username;
        DisplayName = user.DisplayName ?? user.Username;
        Role = user.Role;
        OnAuthStateChanged?.Invoke();
    }

    public void Logout()
    {
        IsAuthenticated = false;
        UserId = 0;
        Username = string.Empty;
        DisplayName = string.Empty;
        Role = UserRole.ReadOnly;
        OnAuthStateChanged?.Invoke();
    }

    public bool HasPermission(string permission)
    {
        if (!IsAuthenticated) return false;

        return permission switch
        {
            Permissions.Read => true,
            Permissions.Export => true,
            Permissions.Write => CanWrite,
            Permissions.Delete => CanDelete,
            Permissions.Import => CanWrite,
            Permissions.Settings => CanWrite,
            Permissions.UserManagement => IsSuperAdmin,
            _ => false
        };
    }

    public string GetRoleDisplayName() => Role switch
    {
        UserRole.SuperAdmin => "Super Admin",
        UserRole.Admin => "Admin",
        UserRole.ReadOnly => "Okuma",
        _ => "Bilinmiyor"
    };

    public string GetRoleColor() => Role switch
    {
        UserRole.SuperAdmin => "error",
        UserRole.Admin => "primary",
        UserRole.ReadOnly => "success",
        _ => "default"
    };
}
