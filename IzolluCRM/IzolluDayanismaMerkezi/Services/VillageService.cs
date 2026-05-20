using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IzolluVakfi.Services;

public class VillageService
{
    private readonly ApplicationDbContext _context;

    public VillageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Village>> GetAllAsync()
    {
        return await _context.Villages
            .OrderBy(v => v.Ad)
            .ToListAsync();
    }

    public async Task<Village?> GetByIdAsync(int id)
    {
        return await _context.Villages.FindAsync(id);
    }

    public async Task<Village> CreateAsync(Village village)
    {
        village.OlusturmaTarihi = DateTime.Now;
        _context.Villages.Add(village);
        await _context.SaveChangesAsync();
        return village;
    }

    public async Task<Village> UpdateAsync(Village village)
    {
        village.GuncellemeTarihi = DateTime.Now;
        _context.Villages.Update(village);
        await _context.SaveChangesAsync();
        return village;
    }

    public async Task DeleteAsync(int id)
    {
        var village = await _context.Villages.FindAsync(id);
        if (village != null)
        {
            _context.Villages.Remove(village);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Villages.CountAsync();
    }

    public async Task<int> GetTotalPopulationAsync()
    {
        return await _context.Villages
            .Where(v => v.Nufus.HasValue)
            .SumAsync(v => v.Nufus!.Value);
    }

    public async Task<int> GetTotalStudentsAsync()
    {
        var villages = await _context.Villages.ToListAsync();
        return villages.Sum(v =>
            (v.IlkokulOgrenciSayisi ?? 0) +
            (v.OrtaokulOgrenciSayisi ?? 0) +
            (v.LiseOgrenciSayisi ?? 0) +
            (v.UniversiteOgrenciSayisi ?? 0)
        );
    }
}
