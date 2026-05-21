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

// Railway PORT env var'ını oku; yoksa 8080 varsayılan (Railway default)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Enable static web assets (for MudBlazor and other RCL packages)
builder.WebHost.UseStaticWebAssets();

// Configure QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Bağlantı kesilince kullanıcıya daha uzun süre bekletme şansı ver
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    options.DisconnectedCircuitMaxRetained = 50;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);
    options.DetailedErrors = false;
})
.AddHubOptions(options =>
{
    // SignalR keep-alive: Railway'in idle timeout'unu aşmak için
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(20);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
});
builder.Services.AddMudServices();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(60);
    });
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add Application Services
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<StudentScholarshipStatusService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<DonorService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<MemberScholarshipCommitmentService>();
builder.Services.AddScoped<SystemSettingsService>();
builder.Services.AddScoped<ScholarshipPaymentService>();
builder.Services.AddScoped<TranscriptService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<VillageService>();
builder.Services.AddScoped<AidService>();
builder.Services.AddScoped<TermService>();
builder.Services.AddScoped<TermReportService>();
builder.Services.AddScoped<RbacService>();

// Scoped per Blazor circuit so each browser session has isolated auth state
builder.Services.AddScoped<AuthService>();

// Add Singleton Services
builder.Services.AddSingleton<TermChangeNotifier>();

// Add Background Jobs
builder.Services.AddHostedService<GradePromotionBackgroundJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// Blazor Hub: tüm transport'lara izin ver (WebSocket öncelikli, kopunca
// otomatik Long Polling'e düşer - mobil network'ler için kritik)
app.MapBlazorHub(options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                       | Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});
app.MapFallbackToPage("/_Host");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Bootstrap: ensure all pre-RBAC migrations are recorded in history
        // This handles both: (1) EnsureCreated DBs with no history, and
        // (2) DBs with partial history (e.g. only the last migration was recorded)
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();

        // Create __EFMigrationsHistory table if it doesn't exist
        using (var createCmd = conn.CreateCommand())
        {
            createCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                    ""ProductVersion"" TEXT NOT NULL
                )";
            await createCmd.ExecuteNonQueryAsync();
        }

        // Check actual table/column existence to decide which RBAC migrations to mark as done.
        // The seed DB was created before RBAC existed (via EnsureCreated), so AppUsers may be
        // missing even though the migration ID looks like it was applied.
        bool appUsersExists;
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AppUsers'";
            appUsersExists = (long)(await checkCmd.ExecuteScalarAsync())! > 0;
        }

        bool lockoutColumnsExist = false;
        if (appUsersExists)
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AppUsers') WHERE name='FailedLoginCount'";
            lockoutColumnsExist = (long)(await checkCmd.ExecuteScalarAsync())! > 0;
        }

        // Ensure every pre-RBAC migration is recorded (INSERT OR IGNORE is safe to repeat)
        var knownMigrationsList = new List<string>
        {
            "20251115192759_InitialCreate",
            "20251115194550_AddFirmaToMember",
            "20251115195621_AddVillagesTable",
            "20251115201531_AddAidsTable",
            "20251115201657_AddAcademicYearToCommitments",
            "20251115211707_AddTermManagementTables",
            "20251115213635_AddAcademicYearToAid",
            "20251115214839_AddScholarshipCutTracking",
            "20251115232931_AddFirmaToStudent",
            "20251210154738_AddMemberPeriodFields",
            "20251216181744_AddIsMalatyaUniversityToStudent",
            "20251216191717_AddMemberMeetingAttendance",
            "20251220100835_AddMeetingNotesAndAttendanceStatus",
            "20251220114049_SyncWithDatabase",
            "20251220120506_AddSektorToMember",
            "20251222140844_AddStudentScholarshipStatus",
            "20251222142453_AddTermIdToMeeting",
            "20260104210357_AddIsMaxGradeReachedFlag",
        };

        // Only mark RBAC migrations as done if the tables/columns actually exist.
        // If AppUsers is missing, let MigrateAsync run AddRbacSystem to create it.
        if (appUsersExists)     knownMigrationsList.Add("20260520000000_AddRbacSystem");
        if (lockoutColumnsExist) knownMigrationsList.Add("20260520000001_AddUserLockout");

        var knownMigrations = knownMigrationsList.ToArray();

        foreach (var migration in knownMigrations)
        {
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = $"INSERT OR IGNORE INTO \"__EFMigrationsHistory\" VALUES ('{migration}', '8.0.0')";
            await insertCmd.ExecuteNonQueryAsync();
        }

        logger.LogInformation("Migration history verified ({Count} baseline migrations ensured).", knownMigrations.Length);
        await conn.CloseAsync();

        // Apply pending migrations (new RBAC migration will run here)
        await context.Database.MigrateAsync();

        // Enable SQLite WAL mode for better concurrency
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store = MEMORY;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA mmap_size = 30000000000;");
        logger.LogInformation("SQLite WAL mode and performance optimizations enabled.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database initialization warning");
        // Fallback to EnsureCreated if migration fails
        context.Database.EnsureCreated();
    }

    var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
    await settingsService.InitializeDefaultSettingsAsync();

    var systemSettingsService = scope.ServiceProvider.GetRequiredService<SystemSettingsService>();
    await systemSettingsService.GetOrCreateSettingsAsync();

    // Seed default admin user if no users exist
    try
    {
        var rbacService = scope.ServiceProvider.GetRequiredService<RbacService>();
        await rbacService.SeedDefaultAdminAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "SeedDefaultAdminAsync failed (AppUsers table may not be ready yet)");
    }
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
                await Task.Delay(1500);
                OpenBrowser("http://localhost:5000");
            }
            catch (Exception ex)
            {
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
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }
    catch { }
}
