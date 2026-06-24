using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class ErrorLogService
{
    private readonly ApplicationDbContext _context;

    public ErrorLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string kaynak, Exception ex, string kullaniciAdi = "admin")
    {
        try
        {
            var log = new ErrorLog
            {
                Tarih = DateTime.Now,
                KullaniciAdi = kullaniciAdi,
                Kaynak = kaynak,
                Mesaj = ErrorMessageHelper.GetFriendlyMessage(ex),
                Detay = ErrorMessageHelper.GetTechnicalDetail(ex)
            };

            _context.ErrorLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Logging must never throw and mask the original error being reported.
        }
    }

    public async Task<List<ErrorLog>> GetRecentAsync(int count = 200)
    {
        return await _context.ErrorLogs
            .OrderByDescending(e => e.Tarih)
            .Take(count)
            .ToListAsync();
    }

    public async Task ClearAsync()
    {
        _context.ErrorLogs.RemoveRange(_context.ErrorLogs);
        await _context.SaveChangesAsync();
    }
}
