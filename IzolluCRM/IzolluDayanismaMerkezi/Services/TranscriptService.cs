using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class TranscriptService
{
    private readonly ApplicationDbContext _context;
    private readonly ActivityLogService _logService;
    private readonly StudentScholarshipStatusService _scholarshipStatusService;

    public TranscriptService(ApplicationDbContext context, ActivityLogService logService, StudentScholarshipStatusService scholarshipStatusService)
    {
        _context = context;
        _logService = logService;
        _scholarshipStatusService = scholarshipStatusService;
    }

    public async Task<List<TranscriptRecord>> GetByStudentIdAsync(int studentId)
    {
        return await _context.TranscriptRecords
            .Where(t => t.StudentId == studentId)
            .OrderByDescending(t => t.GirisTarihi)
            .ToListAsync();
    }

    public async Task<TranscriptRecord> CreateAsync(TranscriptRecord transcript)
    {
        _context.TranscriptRecords.Add(transcript);
        await _context.SaveChangesAsync();

        // Update student's TranskriptNotu with the latest transcript GNO
        var student = await _context.Students.FindAsync(transcript.StudentId);
        if (student != null)
        {
            student.TranskriptNotu = transcript.GNO.ToString("0.00");
            student.GuncellemeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        await _logService.LogAsync("TranskriptEkle", 
            $"Transkript eklendi - Öğrenci ID: {transcript.StudentId}, GNO: {transcript.GNO}");

        return transcript;
    }

    public async Task DeleteAsync(int id)
    {
        var transcript = await _context.TranscriptRecords.FindAsync(id);
        if (transcript != null)
        {
            var studentId = transcript.StudentId;
            
            // PDF dosyasını sil
            if (!string.IsNullOrEmpty(transcript.PdfDosyaYolu))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", transcript.PdfDosyaYolu.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _context.TranscriptRecords.Remove(transcript);
            await _context.SaveChangesAsync();

            // Update student's TranskriptNotu with the new latest transcript GNO
            var latestTranscript = await _context.TranscriptRecords
                .Where(t => t.StudentId == studentId)
                .OrderByDescending(t => t.GirisTarihi)
                .FirstOrDefaultAsync();
            
            var student = await _context.Students.FindAsync(studentId);
            if (student != null)
            {
                student.TranskriptNotu = latestTranscript != null ? latestTranscript.GNO.ToString("0.00") : string.Empty;
                student.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            await _logService.LogAsync("TranskriptSil", 
                $"Transkript silindi - ID: {id}, Öğrenci ID: {studentId}");
        }
    }

    public async Task<decimal?> GetLatestGNOAsync(int studentId)
    {
        var latest = await _context.TranscriptRecords
            .Where(t => t.StudentId == studentId)
            .OrderByDescending(t => t.GirisTarihi)
            .FirstOrDefaultAsync();

        return latest?.GNO;
    }

    public async Task<int> CheckAndUpdateScholarshipsAsync()
    {
        var result = await CheckAndUpdateScholarshipsWithDetailsAsync();
        return result.Count;
    }

    public async Task<List<Student>> CheckAndUpdateScholarshipsWithDetailsAsync()
    {
        var activeStudents = await _context.Students
            .Include(s => s.Transcripts)
            .Where(s => s.AktifBursMu)
            .ToListAsync();

        var affectedStudents = new List<Student>();

        // Get active term for month-based scholarship cutting
        var activeTerm = await _context.Terms.FirstOrDefaultAsync(t => t.IsActive);
        if (activeTerm == null)
        {
            await _logService.LogAsync("TranskriptKontrol", "Aktif dönem bulunamadı. İşlem iptal edildi.");
            return affectedStudents;
        }

        var currentDate = DateTime.Now;
        var currentMonth = currentDate.Month;
        var currentYear = currentDate.Year;

        // Determine scenario: End-of-Term (after May) or Mid-Term (Oct-May)
        bool isEndOfTermScenario = currentMonth > 5 && currentMonth < 10; // June, July, August, September

        foreach (var student in activeStudents)
        {
            var latestTranscript = student.Transcripts
                .OrderByDescending(t => t.GirisTarihi)
                .FirstOrDefault();

            if (latestTranscript != null && latestTranscript.GNO < 2.0m)
            {
                student.AktifBursMu = false;
                student.ScholarshipCutDate = DateTime.Now;
                student.ScholarshipCutReason = "Transkript Kontrolü";
                student.TranskriptNotu = latestTranscript.GNO.ToString("0.00");
                student.GuncellemeTarihi = DateTime.Now;

                var cutReason = $"Transkript GNO < 2.0 ({latestTranscript.GNO:0.00})";

                if (isEndOfTermScenario)
                {
                    // Scenario 1: End-of-Term (After May)
                    // Mark the student's status for the NEXT academic year as 'Burs Kesildi'
                    
                    // Find or create next term
                    var nextTerm = await _context.Terms
                        .Where(t => t.Start > activeTerm.End)
                        .OrderBy(t => t.Start)
                        .FirstOrDefaultAsync();

                    if (nextTerm != null)
                    {
                        // Initialize scholarship statuses for next term if not already exists
                        var nextTermStatuses = await _context.StudentScholarshipStatuses
                            .Where(s => s.StudentId == student.Id && s.TermId == nextTerm.Id)
                            .ToListAsync();

                        if (!nextTermStatuses.Any())
                        {
                            await _scholarshipStatusService.InitializeForStudentAsync(
                                student.Id, nextTerm.Id, student.AylikTutar);
                            
                            nextTermStatuses = await _context.StudentScholarshipStatuses
                                .Where(s => s.StudentId == student.Id && s.TermId == nextTerm.Id)
                                .ToListAsync();
                        }

                        // Mark all months in next term as cut with reason "Transkript Kontrolü"
                        foreach (var status in nextTermStatuses)
                        {
                            status.IsPaid = false;
                            status.CutReason = cutReason;
                            status.UpdatedAt = DateTime.Now;
                            status.UpdatedBy = "Transkript Kontrolü";
                        }

                        await _logService.LogAsync("BursKesildi", 
                            $"{student.AdSoyad} - Transkript Kontrolü (Gelecek Dönem) - GNO: {latestTranscript.GNO}");
                    }
                    else
                    {
                        await _logService.LogAsync("BursKesildi", 
                            $"{student.AdSoyad} - Transkript Kontrolü - GNO: {latestTranscript.GNO} (Gelecek dönem bulunamadı)");
                    }
                }
                else
                {
                    // Scenario 2: Mid-Term (Oct-May)
                    // Update all future months in the current term to 'Kesildi'
                    
                    var currentTermStatuses = await _context.StudentScholarshipStatuses
                        .Where(s => s.StudentId == student.Id && s.TermId == activeTerm.Id)
                        .ToListAsync();

                    if (!currentTermStatuses.Any())
                    {
                        // Initialize if not exists
                        await _scholarshipStatusService.InitializeForStudentAsync(
                            student.Id, activeTerm.Id, student.AylikTutar);
                        
                        currentTermStatuses = await _context.StudentScholarshipStatuses
                            .Where(s => s.StudentId == student.Id && s.TermId == activeTerm.Id)
                            .ToListAsync();
                    }

                    // Filter: Select all months AFTER the current month
                    // For academic year logic: Oct(10)-Dec(12) are in year N, Jan(1)-May(5) are in year N+1
                    foreach (var status in currentTermStatuses)
                    {
                        bool isFutureMonth = false;

                        // Check if this status record is for a future month
                        if (status.Year > currentYear)
                        {
                            isFutureMonth = true;
                        }
                        else if (status.Year == currentYear && status.Month > currentMonth)
                        {
                            isFutureMonth = true;
                        }

                        if (isFutureMonth)
                        {
                            status.IsPaid = false;
                            status.CutReason = cutReason;
                            status.UpdatedAt = DateTime.Now;
                            status.UpdatedBy = "Transkript Kontrolü";
                        }
                    }

                    await _logService.LogAsync("BursKesildi", 
                        $"{student.AdSoyad} - Transkript Kontrolü (Gelecek Aylar) - GNO: {latestTranscript.GNO}");
                }

                affectedStudents.Add(student);
            }
        }

        if (affectedStudents.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        await _logService.LogAsync("TranskriptKontrol", 
            $"Transkript kontrolü tamamlandı. {affectedStudents.Count} öğrencinin bursu kesildi.");

        return affectedStudents;
    }

    /// <summary>
    /// Gets students with active scholarship whose latest transcript GNO is below 2.0
    /// </summary>
    public async Task<List<Student>> GetStudentsWithLowGnoAsync()
    {
        var activeStudents = await _context.Students
            .Include(s => s.Transcripts)
            .Where(s => s.AktifBursMu)
            .ToListAsync();

        var lowGnoStudents = new List<Student>();

        foreach (var student in activeStudents)
        {
            var latestTranscript = student.Transcripts
                .OrderByDescending(t => t.GirisTarihi)
                .FirstOrDefault();

            if (latestTranscript != null && latestTranscript.GNO < 2.0m)
            {
                lowGnoStudents.Add(student);
            }
        }

        return lowGnoStudents;
    }
}