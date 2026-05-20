namespace IzolluVakfi.Services;

public class AuthService
{
    // Kullanici adi ve sifre environment variable'dan okunur
    // Railway/Docker: APP_USERNAME ve APP_PASSWORD degiskenlerini tanimlayin
    // Lokal: appsettings.Development.json veya env var ile override edin
    private readonly string _username;
    private readonly string _password;

    public AuthService()
    {
        _username = Environment.GetEnvironmentVariable("APP_USERNAME") ?? "izollu";
        _password = Environment.GetEnvironmentVariable("APP_PASSWORD") ?? "changeme";
    }

    public bool IsAuthenticated { get; private set; }

    public event Action? OnAuthStateChanged;

    public bool Login(string username, string password)
    {
        if (username == _username && password == _password)
        {
            IsAuthenticated = true;
            OnAuthStateChanged?.Invoke();
            return true;
        }
        return false;
    }
    
    public void Logout()
    {
        IsAuthenticated = false;
        OnAuthStateChanged?.Invoke();
    }
}
