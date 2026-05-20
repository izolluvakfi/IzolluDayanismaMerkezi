using IzolluVakfi.Data;
using Microsoft.EntityFrameworkCore;

namespace IzolluVakfi.Services;

/// <summary>
/// Background job that automatically promotes students to the next grade level every August 1st.
/// Increments Sinif (class level) by 1 for all active students who are not yet graduated.
/// Students already in Grade 4 should be manually graduated using the "Mezun Et" operation.
/// </summary>
public class GradePromotionBackgroundJob : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GradePromotionBackgroundJob> _logger;
    private Timer? _timer;

    public GradePromotionBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<GradePromotionBackgroundJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Grade Promotion Background Job başlatıldı.");

        // Schedule the job to run daily at 00:01 AM
        var now = DateTime.Now;
        var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 0, 1, 0);
        
        // If today's scheduled time has passed, schedule for tomorrow
        if (now > scheduledTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        var initialDelay = scheduledTime - now;
        
        _timer = new Timer(
            callback: async _ => await ExecuteJobAsync(),
            state: null,
            dueTime: initialDelay,
            period: TimeSpan.FromDays(1) // Run daily
        );

        return Task.CompletedTask;
    }

    private async Task ExecuteJobAsync()
    {
        var today = DateTime.Today;
        
        // Only run on August 1st
        if (today.Month != 8 || today.Day != 1)
        {
            return;
        }

        _logger.LogInformation($"Sınıf atlama işlemi başlatılıyor - {today:dd.MM.yyyy}");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Fetch all active students (not graduated)
            var activeStudents = await context.Students
                .Where(s => !s.MezunMu && s.Sinif.HasValue)
                .ToListAsync();

            if (activeStudents.Count == 0)
            {
                _logger.LogInformation("Sınıf atlatılacak öğrenci bulunamadı.");
                return;
            }

            var promotedCount = 0;
            var grade4FlaggedCount = 0;

            // Start transaction for data consistency
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                foreach (var student in activeStudents)
                {
                    if (student.Sinif == 4)
                    {
                        // Students in Grade 4 should NOT be promoted to 5
                        // Instead, flag them as having reached maximum grade
                        if (!student.IsMaxGradeReached)
                        {
                            student.IsMaxGradeReached = true;
                            student.GuncellemeTarihi = DateTime.Now;
                            grade4FlaggedCount++;
                            
                            _logger.LogInformation(
                                $"Öğrenci {student.AdSoyad} (Sicil: {student.SicilNumarasi}) " +
                                $"4. sınıfta maksimum seviyeye ulaştı. İşlem gerekli olarak işaretlendi.");
                        }
                        continue;
                    }

                    // Increment class level for grades 1-3
                    student.Sinif++;
                    student.GuncellemeTarihi = DateTime.Now;
                    promotedCount++;

                    _logger.LogInformation(
                        $"Öğrenci {student.AdSoyad} (Sicil: {student.SicilNumarasi}) " +
                        $"{student.Sinif - 1}. sınıftan {student.Sinif}. sınıfa atlatıldı.");
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    $"Sınıf atlama işlemi tamamlandı. " +
                    $"Atlatılan öğrenci sayısı: {promotedCount}, " +
                    $"4. sınıfta maksimum seviyeye ulaşan öğrenci sayısı: {grade4FlaggedCount}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Sınıf atlama işlemi sırasında hata oluştu. Transaction geri alındı.");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sınıf atlama background job çalıştırılırken hata oluştu.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Grade Promotion Background Job durduruluyor.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
