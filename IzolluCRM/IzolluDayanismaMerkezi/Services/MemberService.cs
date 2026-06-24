using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class MemberService
{
    private readonly ApplicationDbContext _context;
    private readonly ActivityLogService _logService;

    public MemberService(ApplicationDbContext context, ActivityLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    // Legacy methods - kept for backward compatibility
    public async Task<List<Member>> GetAllAsync()
    {
        return await _context.Members
            .OrderBy(m => m.AdSoyad)
            .ToListAsync();
    }

    public async Task<List<Member>> GetMutevelliAsync()
    {
        return await _context.Members
            .Where(m => m.IsMutevelli)
            .OrderBy(m => m.AdSoyad)
            .ToListAsync();
    }

    public async Task<List<Member>> GetYonetimKuruluAsync()
    {
        return await _context.Members
            .Where(m => m.IsYonetimKurulu)
            .OrderBy(m => m.AdSoyad)
            .ToListAsync();
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        return await _context.Members.FindAsync(id);
    }

    public async Task<string> GetNextSicilNumarasiAsync()
    {
        const int startingSicil = 10001;
        
        var lastMember = await _context.Members
            .Where(m => m.SicilNumarasi != null && m.SicilNumarasi.StartsWith("1"))
            .OrderByDescending(m => m.SicilNumarasi)
            .FirstOrDefaultAsync();
        
        if (lastMember == null || string.IsNullOrEmpty(lastMember.SicilNumarasi))
        {
            return startingSicil.ToString();
        }
        
        if (int.TryParse(lastMember.SicilNumarasi, out int lastSicil))
        {
            return (lastSicil + 1).ToString();
        }
        
        return startingSicil.ToString();
    }

    public async Task<Member> CreateAsync(Member member)
    {
        // Auto-generate sicil if not provided
        if (string.IsNullOrWhiteSpace(member.SicilNumarasi))
        {
            member.SicilNumarasi = await GetNextSicilNumarasiAsync();
        }
        else
        {
            member.SicilNumarasi = NormalizeSicil(member.SicilNumarasi);
        }

        if (!string.IsNullOrEmpty(member.SicilNumarasi))
        {
            var existingMember = await _context.Members
                .FirstOrDefaultAsync(m => m.SicilNumarasi == member.SicilNumarasi);

            if (existingMember != null)
            {
                throw new InvalidOperationException($"Bu sicil numarası ({member.SicilNumarasi}) zaten kayıtlı!");
            }
        }

        _context.Members.Add(member);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(ErrorMessageHelper.GetFriendlyMessage(ex), ex);
        }

        await _logService.LogAsync("UyeEkle", $"Yeni üye eklendi: {member.AdSoyad}");

        return member;
    }

    public async Task<Member> UpdateAsync(Member member)
    {
        member.SicilNumarasi = NormalizeSicil(member.SicilNumarasi);

        if (!string.IsNullOrEmpty(member.SicilNumarasi))
        {
            var existingMember = await _context.Members
                .FirstOrDefaultAsync(m => m.SicilNumarasi == member.SicilNumarasi && m.Id != member.Id);

            if (existingMember != null)
            {
                throw new InvalidOperationException($"Bu sicil numarası ({member.SicilNumarasi}) zaten kayıtlı!");
            }
        }

        _context.Members.Update(member);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("UyeGuncelle", $"Üye güncellendi: {member.AdSoyad}");

        return member;
    }

    public async Task DeleteAsync(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member != null)
        {
            try
            {
                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is InvalidOperationException)
            {
                _context.Entry(member).State = EntityState.Unchanged;
                throw new InvalidOperationException(
                    $"{member.AdSoyad} silinemedi: {ErrorMessageHelper.GetFriendlyMessage(ex)}", ex);
            }

            await _logService.LogAsync("UyeSil", $"Üye silindi: {member.AdSoyad}");
        }
    }

    public async Task ToggleMutevelliAsync(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member != null)
        {
            member.IsMutevelli = !member.IsMutevelli;
            await _context.SaveChangesAsync();

            await _logService.LogAsync("MutevelliDegisiklik", 
                $"{member.AdSoyad} - Mütevelli: {member.IsMutevelli}");
        }
    }

    public async Task ToggleYonetimKuruluAsync(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member != null)
        {
            member.IsYonetimKurulu = !member.IsYonetimKurulu;
            await _context.SaveChangesAsync();

            await _logService.LogAsync("YonetimKuruluDegisiklik", 
                $"{member.AdSoyad} - Yönetim Kurulu: {member.IsYonetimKurulu}");
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Members.CountAsync(m => m.AktifMi);
    }

    public async Task<int> GetBursVerenCountAsync()
    {
        return await _context.Members.CountAsync(m => m.BursVeriyor && m.AktifMi);
    }

    /// <summary>
    /// Alias for GetBursVerenCountAsync - counts active members providing scholarships
    /// This matches the "Bağışçılar" tab data in Members page
    /// </summary>
    public async Task<int> GetDonorCountAsync()
    {
        return await GetBursVerenCountAsync();
    }

    public async Task<Dictionary<string, decimal>> GetTotalScholarshipByMemberAsync()
    {
        var members = await _context.Members
            .Where(m => m.BursVeriyor && m.AktifMi)
            .ToListAsync();

        var students = await _context.Students
            .Where(s => !string.IsNullOrEmpty(s.BagisciAdi))
            .ToListAsync();

        var memberScholarships = new Dictionary<string, decimal>();

        foreach (var member in members)
        {
            var totalAmount = students
                .Where(s => s.BagisciAdi != null && s.BagisciAdi.Equals(member.AdSoyad, StringComparison.OrdinalIgnoreCase))
                .Sum(s => s.ToplamAlinanBurs);

            if (totalAmount > 0)
            {
                memberScholarships[member.AdSoyad] = totalAmount;
            }
        }

        return memberScholarships
            .OrderByDescending(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private static string? NormalizeSicil(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return trimmed.ToUpperInvariant();
    }
}