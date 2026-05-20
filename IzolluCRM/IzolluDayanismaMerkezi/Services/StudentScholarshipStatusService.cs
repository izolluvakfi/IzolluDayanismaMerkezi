using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class StudentScholarshipStatusService
{
    private readonly ApplicationDbContext _context;

    public StudentScholarshipStatusService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all scholarship statuses for a student in a specific term
    /// </summary>
    public async Task<List<StudentScholarshipStatus>> GetByStudentAndTermAsync(int studentId, int termId)
    {
        return await _context.StudentScholarshipStatuses
            .Where(s => s.StudentId == studentId && s.TermId == termId)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month == 10 || s.Month == 11 || s.Month == 12 ? s.Month : s.Month + 12) // Order: Oct, Nov, Dec, Jan, Feb, Mar, Apr, May
            .ToListAsync();
    }

    /// <summary>
    /// Get all scholarship statuses for a specific term (all students)
    /// </summary>
    public async Task<List<StudentScholarshipStatus>> GetAllByTermAsync(int termId)
    {
        return await _context.StudentScholarshipStatuses
            .Where(s => s.TermId == termId)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month == 10 || s.Month == 11 || s.Month == 12 ? s.Month : s.Month + 12)
            .ToListAsync();
    }

    /// <summary>
    /// Initialize scholarship statuses for a student for a term (8 months: Oct-May)
    /// </summary>
    public async Task<List<StudentScholarshipStatus>> InitializeForStudentAsync(int studentId, int termId, decimal monthlyAmount)
    {
        // Get term to determine years
        var term = await _context.Terms.FindAsync(termId);
        if (term == null)
            throw new Exception("Dönem bulunamadı.");

        // Parse start year from term
        var startYear = term.Start.Year;

        var existingStatuses = await _context.StudentScholarshipStatuses
            .Where(s => s.StudentId == studentId && s.TermId == termId)
            .ToListAsync();

        // Create status records for each scholarship month (Oct - May)
        var scholarshipMonths = StudentScholarshipStatus.GetScholarshipMonths();
        var newStatuses = new List<StudentScholarshipStatus>();
        
        foreach (var (month, yearOffset) in scholarshipMonths)
        {
            var year = startYear + yearOffset;
            
            // Check if already exists
            var existing = existingStatuses.FirstOrDefault(s => s.Month == month && s.Year == year);
            if (existing == null)
            {
                // Determine if this month should be auto-marked as paid
                // Logic: If the scholarship month is in the past or current month, mark as paid
                var scholarshipDate = new DateTime(year, month, 1);
                var now = DateTime.Now;
                bool isCurrentOrPast = scholarshipDate.Year < now.Year || 
                                      (scholarshipDate.Year == now.Year && scholarshipDate.Month <= now.Month);
                
                var status = new StudentScholarshipStatus
                {
                    StudentId = studentId,
                    TermId = termId,
                    Month = month,
                    Year = year,
                    IsPaid = isCurrentOrPast, // Auto-mark past/current months as paid
                    Amount = monthlyAmount,
                    CreatedAt = DateTime.Now
                };
                _context.StudentScholarshipStatuses.Add(status);
                newStatuses.Add(status);
            }
            else
            {
                // EXISTING RECORD CHECK:
                // If the month is current or past, AND it is NOT paid, AND it has NO cut reason (meaning it's just pending),
                // automatically mark it as Paid.
                
                var scholarshipDate = new DateTime(year, month, 1);
                var now = DateTime.Now;
                bool isCurrentOrPast = scholarshipDate < new DateTime(now.Year, now.Month, 1).AddMonths(1); 
                // Using strictly less than next month start covers current month and past.
                
                if (isCurrentOrPast && !existing.IsPaid && string.IsNullOrEmpty(existing.CutReason))
                {
                    existing.IsPaid = true;
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = "Auto-Payment (System)";
                }
            }
        }

        if (newStatuses.Any())
        {
            await _context.SaveChangesAsync();
        }

        return await GetByStudentAndTermAsync(studentId, termId);
    }

    /// <summary>
    /// Toggle scholarship status for a specific month
    /// </summary>
    public async Task<StudentScholarshipStatus> ToggleStatusAsync(int statusId, bool isPaid, string? cutReason = null, string? updatedBy = null)
    {
        var status = await _context.StudentScholarshipStatuses.FindAsync(statusId);
        if (status == null)
            throw new Exception("Burs durumu kaydı bulunamadı.");

        status.IsPaid = isPaid;
        status.CutReason = isPaid ? null : cutReason;
        status.UpdatedAt = DateTime.Now;
        status.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
        return status;
    }

    /// <summary>
    /// Cut scholarship for a specific month based on meeting date
    /// </summary>
    public async Task<(bool Success, string Message, StudentScholarshipStatus? Status)> CutScholarshipByMeetingDateAsync(
        int studentId, 
        int termId, 
        DateTime meetingDate, 
        string cutReason,
        string? updatedBy = null)
    {
        var month = meetingDate.Month;
        
        // Check if month is in scholarship period (Oct-May)
        if (!StudentScholarshipStatus.IsScholarshipMonth(month))
        {
            return (false, $"Toplantı tarihi ({meetingDate:dd.MM.yyyy}) burs dönemine (Ekim-Mayıs) dahil değil. İşlem yapılmadı.", null);
        }

        // Get term to determine year
        var term = await _context.Terms.FindAsync(termId);
        if (term == null)
            return (false, "Dönem bulunamadı.", null);

        var startYear = term.Start.Year;

        // Determine the correct year for this month
        var year = (month >= 10) ? startYear : startYear + 1;

        // Find the status record
        var status = await _context.StudentScholarshipStatuses
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TermId == termId && s.Month == month && s.Year == year);

        if (status == null)
        {
            // Initialize if not exists
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return (false, "Öğrenci bulunamadı.", null);

            await InitializeForStudentAsync(studentId, termId, student.AylikTutar);
            
            status = await _context.StudentScholarshipStatuses
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TermId == termId && s.Month == month && s.Year == year);
        }

        if (status == null)
            return (false, "Burs durumu kaydı oluşturulamadı.", null);

        // Cut the scholarship
        status.IsPaid = false;
        status.CutReason = cutReason;
        status.UpdatedAt = DateTime.Now;
        status.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        var monthName = status.GetMonthName();
        return (true, $"{monthName} {year} ayı bursu kesildi. Sebep: {cutReason}", status);
    }

    /// <summary>
    /// Bulk cut scholarships for multiple students for a specific month
    /// </summary>
    public async Task<List<(int StudentId, bool Success, string Message)>> BulkCutScholarshipsAsync(
        List<int> studentIds,
        int termId,
        DateTime meetingDate,
        string cutReason,
        string? updatedBy = null)
    {
        var results = new List<(int StudentId, bool Success, string Message)>();

        foreach (var studentId in studentIds)
        {
            var result = await CutScholarshipByMeetingDateAsync(studentId, termId, meetingDate, cutReason, updatedBy);
            results.Add((studentId, result.Success, result.Message));
        }

        return results;
    }

    /// <summary>
    /// Get summary statistics for a term
    /// </summary>
    public async Task<ScholarshipTermSummary> GetTermSummaryAsync(int termId)
    {
        var statuses = await _context.StudentScholarshipStatuses
            .Where(s => s.TermId == termId)
            .ToListAsync();

        var summary = new ScholarshipTermSummary
        {
            TermId = termId,
            TotalMonthlyRecords = statuses.Count,
            PaidCount = statuses.Count(s => s.IsPaid),
            CutCount = statuses.Count(s => !s.IsPaid),
            TotalPaidAmount = statuses.Where(s => s.IsPaid).Sum(s => s.Amount),
            TotalCutAmount = statuses.Where(s => !s.IsPaid).Sum(s => s.Amount),
            CutReasonBreakdown = statuses
                .Where(s => !s.IsPaid && !string.IsNullOrEmpty(s.CutReason))
                .GroupBy(s => s.CutReason)
                .ToDictionary(g => g.Key!, g => g.Count())
        };

        return summary;
    }

    /// <summary>
    /// Get student's scholarship summary for a term
    /// </summary>
    public async Task<StudentScholarshipSummary> GetStudentSummaryAsync(int studentId, int termId)
    {
        var statuses = await _context.StudentScholarshipStatuses
            .Where(s => s.StudentId == studentId && s.TermId == termId)
            .ToListAsync();

        return new StudentScholarshipSummary
        {
            StudentId = studentId,
            TermId = termId,
            TotalMonths = statuses.Count,
            PaidMonths = statuses.Count(s => s.IsPaid),
            CutMonths = statuses.Count(s => !s.IsPaid),
            TotalPaidAmount = statuses.Where(s => s.IsPaid).Sum(s => s.Amount),
            TotalCutAmount = statuses.Where(s => !s.IsPaid).Sum(s => s.Amount),
            Statuses = statuses
        };
    }

    /// <summary>
    /// Infer the active term from a date
    /// </summary>
    public async Task<Term?> InferTermFromDateAsync(DateTime date)
    {
        // Find term where date falls within start and end
        var term = await _context.Terms
            .Where(t => date >= t.Start && date <= t.End)
            .FirstOrDefaultAsync();

        // If not found, get the active term
        if (term == null)
        {
            term = await _context.Terms
                .Where(t => t.IsActive)
                .FirstOrDefaultAsync();
        }

        return term;
    }
}

public class ScholarshipTermSummary
{
    public int TermId { get; set; }
    public int TotalMonthlyRecords { get; set; }
    public int PaidCount { get; set; }
    public int CutCount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalCutAmount { get; set; }
    public Dictionary<string, int> CutReasonBreakdown { get; set; } = new();
}

public class StudentScholarshipSummary
{
    public int StudentId { get; set; }
    public int TermId { get; set; }
    public int TotalMonths { get; set; }
    public int PaidMonths { get; set; }
    public int CutMonths { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalCutAmount { get; set; }
    public List<StudentScholarshipStatus> Statuses { get; set; } = new();
}
