using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class StudentService
{
    private readonly ApplicationDbContext _context;
    private readonly ActivityLogService _logService;
    private readonly MeetingService _meetingService;

    public StudentService(ApplicationDbContext context, ActivityLogService logService, MeetingService meetingService)
    {
        _context = context;
        _logService = logService;
        _meetingService = meetingService;
    }

    // Legacy methods - kept for backward compatibility
    public async Task<List<Student>> GetAllAsync()
    {
        return await _context.Students
            .Include(s => s.Transcripts)
            .OrderBy(s => s.AdSoyad)
            .ToListAsync();
    }

    public async Task<List<Student>> GetCurrentStudentsAsync()
    {
        return await _context.Students
            .Include(s => s.Transcripts)
            .Where(s => !s.MezunMu)
            .OrderBy(s => s.AdSoyad)
            .ToListAsync();
    }

    public async Task<List<Student>> GetActiveScholarshipStudentsAsync()
    {
        return await _context.Students
            .Include(s => s.Transcripts)
            .Where(s => s.AktifBursMu)
            .OrderBy(s => s.AdSoyad)
            .ToListAsync();
    }

    public async Task<List<Student>> GetGraduatedStudentsAsync()
    {
        return await _context.Students
            .Where(s => s.MezunMu)
            .OrderByDescending(s => s.MezuniyetTarihi)
            .ToListAsync();
    }

    public async Task<Student?> GetByIdAsync(int id)
    {
        return await _context.Students
            .Include(s => s.Transcripts)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<string> GetNextSicilNumarasiAsync()
    {
        const int startingSicil = 20001;
        
        var lastStudent = await _context.Students
            .Where(s => s.SicilNumarasi != null && s.SicilNumarasi.StartsWith("2"))
            .OrderByDescending(s => s.SicilNumarasi)
            .FirstOrDefaultAsync();
        
        if (lastStudent == null || string.IsNullOrEmpty(lastStudent.SicilNumarasi))
        {
            return startingSicil.ToString();
        }
        
        if (int.TryParse(lastStudent.SicilNumarasi, out int lastSicil))
        {
            return (lastSicil + 1).ToString();
        }
        
        return startingSicil.ToString();
    }

    public async Task<Student> CreateAsync(Student student)
    {
        // Auto-generate sicil if not provided
        if (string.IsNullOrWhiteSpace(student.SicilNumarasi))
        {
            student.SicilNumarasi = await GetNextSicilNumarasiAsync();
        }
        else
        {
            student.SicilNumarasi = NormalizeSicil(student.SicilNumarasi);
        }

        if (!string.IsNullOrEmpty(student.SicilNumarasi))
        {
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.SicilNumarasi == student.SicilNumarasi);

            if (existingStudent != null)
            {
                throw new InvalidOperationException($"Bu sicil numarası ({student.SicilNumarasi}) zaten kayıtlı!");
            }
        }

        student.OlusturmaTarihi = DateTime.Now;
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        await _meetingService.SeedAttendanceForNewStudentAsync(student.Id);

        await _logService.LogAsync("OgrenciEkle", $"Yeni öğrenci eklendi: {student.AdSoyad}");

        return student;
    }

    public async Task<Student> UpdateAsync(Student student)
    {
        student.SicilNumarasi = NormalizeSicil(student.SicilNumarasi);

        if (!string.IsNullOrEmpty(student.SicilNumarasi))
        {
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.SicilNumarasi == student.SicilNumarasi && s.Id != student.Id);

            if (existingStudent != null)
            {
                throw new InvalidOperationException($"Bu sicil numarası ({student.SicilNumarasi}) zaten kayıtlı!");
            }
        }

        // Get the original student to check if scholarship status changed
        var originalStudent = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == student.Id);
        
        // If scholarship is being reactivated, clear cut information
        if (originalStudent != null && !originalStudent.AktifBursMu && student.AktifBursMu)
        {
            student.ScholarshipCutReason = null;
            student.ScholarshipCutDate = null;
        }

        student.GuncellemeTarihi = DateTime.Now;
        _context.Students.Update(student);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("OgrenciGuncelle", $"Öğrenci güncellendi: {student.AdSoyad}");

        return student;
    }

    public async Task DeleteAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            await _logService.LogAsync("OgrenciSil", $"Öğrenci silindi: {student.AdSoyad}");
        }
    }

    public async Task GraduateStudentAsync(int id, DateTime graduationDate, string? meslek = null, string? firma = null)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            student.MezunMu = true;
            student.MezuniyetTarihi = graduationDate;
            student.AktifBursMu = false;
            
            // Mezuniyet sırasında girilen meslek ve firma bilgilerini kaydet
            if (!string.IsNullOrWhiteSpace(meslek))
                student.Meslek = meslek;
            
            if (!string.IsNullOrWhiteSpace(firma))
                student.Firma = firma;
            
            student.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            await _logService.LogAsync("OgrenciMezun", $"Öğrenci mezun edildi: {student.AdSoyad} - Meslek: {student.Meslek ?? "Belirtilmedi"} - Firma: {student.Firma ?? "Belirtilmedi"}");
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Students.CountAsync();
    }

    public async Task<int> GetActiveScholarshipCountAsync()
    {
        return await _context.Students.CountAsync(s => s.AktifBursMu);
    }

    public async Task<int> GetGraduatedCountAsync()
    {
        return await _context.Students.CountAsync(s => s.MezunMu);
    }

    public async Task<decimal> GetTotalActiveScholarshipAmountAsync()
    {
        // SQLite doesn't support Sum on decimal, so we load into memory first
        var activeStudents = await _context.Students
            .Where(s => s.AktifBursMu)
            .Select(s => s.AylikTutar)
            .ToListAsync();
        
        return activeStudents.Sum();
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

    /// <summary>
    /// Cuts scholarship for a student with the given reason
    /// </summary>
    public async Task CutScholarshipAsync(int studentId, string reason)
    {
        var student = await _context.Students.FindAsync(studentId);
        if (student == null) return;

        student.AktifBursMu = false;
        student.ScholarshipCutReason = reason;
        student.ScholarshipCutDate = DateTime.Now;
        student.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        await _logService.LogAsync("BursKesildi", 
            $"{student.AdSoyad} - Burs kesildi: {reason}");
    }
}