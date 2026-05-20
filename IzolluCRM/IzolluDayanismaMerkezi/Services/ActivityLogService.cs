using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class ActivityLogService
{
    private readonly ApplicationDbContext _context;

    public ActivityLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string islemTipi, string detay, string kullaniciAdi = "admin")
    {
        var log = new ActivityLog
        {
            KullaniciAdi = kullaniciAdi,
            IslemTipi = islemTipi,
            Detay = detay,
            Tarih = DateTime.Now
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ActivityLog>> GetAllAsync()
    {
        return await _context.ActivityLogs
            .OrderByDescending(a => a.Tarih)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.ActivityLogs
            .Where(a => a.Tarih >= startDate && a.Tarih <= endDate)
            .OrderByDescending(a => a.Tarih)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByTypeAsync(string islemTipi)
    {
        return await _context.ActivityLogs
            .Where(a => a.IslemTipi == islemTipi)
            .OrderByDescending(a => a.Tarih)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByPersonNameAsync(string adSoyad)
    {
        return await _context.ActivityLogs
            .Where(a => a.Detay.Contains(adSoyad))
            .OrderByDescending(a => a.Tarih)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByUserAsync(string kullaniciAdi)
    {
        return await _context.ActivityLogs
            .Where(a => a.KullaniciAdi == kullaniciAdi)
            .OrderByDescending(a => a.Tarih)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctUsersAsync()
    {
        return await _context.ActivityLogs
            .Select(a => a.KullaniciAdi)
            .Distinct()
            .OrderBy(u => u)
            .ToListAsync();
    }
}