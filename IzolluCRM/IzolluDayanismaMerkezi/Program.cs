using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using IzolluVakfi.Data;
using IzolluVakfi.Services;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure Turkish culture for dd/MM/yyyy date format
var cultureInfo = new CultureInfo("tr-TR");
cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
// SVG ve chart componentleri için ondalık ayırıcı nokta olmalı
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configure Kestrel to listen on all network interfaces
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000); // HTTP
    // Uncomment for HTTPS: serverOptions.ListenAnyIP(5001, listenOptions => { listenOptions.UseHttps(); });
});

// Enable static web assets (for MudBlazor and other RCL packages)
// Enabled unconditionally to ensure _content/* paths work from any domain
builder.WebHost.UseStaticWebAssets();

// Configure QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(60); // 60 seconds timeout
    });
    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add Application Services
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<StudentScholarshipStatusService>(); // Must be before MeetingService and TranscriptService
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<DonorService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<MemberScholarshipCommitmentService>();
builder.Services.AddScoped<SystemSettingsService>(); // System-wide settings
builder.Services.AddScoped<ScholarshipPaymentService>(); // Payment tracking
builder.Services.AddScoped<TranscriptService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<VillageService>();
builder.Services.AddScoped<AidService>();
builder.Services.AddScoped<TermService>();
builder.Services.AddScoped<TermReportService>();

// Add Singleton Services
builder.Services.AddSingleton<TermChangeNotifier>(); // Cross-component event notifications
builder.Services.AddSingleton<AuthService>(); // Authentication service

// Add Background Jobs
builder.Services.AddHostedService<GradePromotionBackgroundJob>(); // Automatic grade promotion on August 1st

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Create database if it doesn't exist and initialize settings
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    // Enable SQLite WAL mode for better concurrency and performance
    try
    {
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store = MEMORY;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA mmap_size = 30000000000;");
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("SQLite WAL mode and performance optimizations enabled");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to enable SQLite WAL mode");
    }
    
    var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
    await settingsService.InitializeDefaultSettingsAsync();
    
    // Initialize system settings (creates if not exists)
    var systemSettingsService = scope.ServiceProvider.GetRequiredService<SystemSettingsService>();
    await systemSettingsService.GetOrCreateSettingsAsync();
}

// Open browser automatically after startup (only when running locally)
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("OPEN_BROWSER") == "true")
{
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        Task.Run(async () =>
        {
            try
            {
                // Wait for server to be fully ready
                await Task.Delay(1500);
                OpenBrowser("http://localhost:5000");
            }
            catch (Exception ex)
            {
                // Silently fail if browser can't open
                Debug.WriteLine($"Failed to open browser: {ex.Message}");
            }
        });
    });
}

app.Run();

static void OpenBrowser(string url)
{
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
    catch
    {
        // Browser açılamazsa sessizce devam et
    }
}
