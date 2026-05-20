using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IzolluVakfi.Services;

public class MeetingService
{
    private readonly ApplicationDbContext _context;
    private readonly ActivityLogService _logService;
    private readonly StudentScholarshipStatusService _scholarshipStatusService;

    public MeetingService(ApplicationDbContext context, ActivityLogService logService, StudentScholarshipStatusService scholarshipStatusService)
    {
        _context = context;
        _logService = logService;
        _scholarshipStatusService = scholarshipStatusService;
    }

    public async Task<List<Meeting>> GetAllAsync()
    {
        return await _context.Meetings
            .Include(m => m.Attendances)
                .ThenInclude(a => a.Student)
            .Include(m => m.MemberAttendances)
                .ThenInclude(a => a.Member)
            .OrderByDescending(m => m.Tarih)
            .ToListAsync();
    }

    public async Task<Meeting?> GetByIdAsync(int id)
    {
        return await _context.Meetings
            .Include(m => m.Attendances)
                .ThenInclude(a => a.Student)
            .Include(m => m.MemberAttendances)
                .ThenInclude(a => a.Member)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Meeting> CreateAsync(Meeting meeting)
    {
        meeting.OlusturmaTarihi = DateTime.Now;
        if (meeting.BitisTarihi <= meeting.Tarih)
        {
            meeting.BitisTarihi = meeting.Tarih.AddHours(1);
        }
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        await EnsureAttendanceRecordsAsync(meeting.Id, meeting.ToplantiTuru);
        await _logService.LogAsync("ToplantiEkle", $"Yeni toplantı oluşturuldu: {meeting.Baslik}");
        return meeting;
    }

    public async Task DeleteAsync(int meetingId)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null)
            return;

        _context.Meetings.Remove(meeting);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("ToplantiSil", $"Toplantı silindi: {meeting.Baslik}");
    }

    public async Task UpdateAsync(Meeting meeting)
    {
        var existing = await _context.Meetings.FindAsync(meeting.Id);
        if (existing == null)
            return;

        existing.Baslik = meeting.Baslik;
        existing.ToplantiTuru = meeting.ToplantiTuru;
        existing.Konum = meeting.Konum;
        existing.Tarih = meeting.Tarih;
        existing.BitisTarihi = meeting.BitisTarihi;
        existing.Aciklama = meeting.Aciklama;
        existing.TermId = meeting.TermId;

        await _context.SaveChangesAsync();
        await _logService.LogAsync("ToplantiGuncelle", $"Toplantı güncellendi: {meeting.Baslik}");
    }

    public async Task<List<StudentMeetingAttendance>> GetAttendancesForStudentAsync(int studentId)
    {
        return await _context.StudentMeetingAttendances
            .Include(a => a.Meeting)
            .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.Meeting.Tarih)
            .ToListAsync();
    }

    public async Task UpdateAttendanceAsync(int attendanceId, bool katildi)
    {
        var attendance = await _context.StudentMeetingAttendances.FindAsync(attendanceId);
        if (attendance == null)
            return;

        attendance.Katildi = katildi;
        attendance.GuncellemeTarihi = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMemberAttendanceAsync(int attendanceId, bool katildi)
    {
        var attendance = await _context.MemberMeetingAttendances.FindAsync(attendanceId);
        if (attendance == null)
            return;

        attendance.Katildi = katildi;
        attendance.GuncellemeTarihi = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task SeedAttendanceForNewStudentAsync(int studentId)
    {
        var meetingIds = await _context.Meetings.Select(m => m.Id).ToListAsync();
        if (!meetingIds.Any())
            return;

        var newRecords = meetingIds.Select(id => new StudentMeetingAttendance
        {
            StudentId = studentId,
            MeetingId = id,
            Katildi = true
        });

        _context.StudentMeetingAttendances.AddRange(newRecords);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureAttendanceRecordsAsync(int meetingId, string toplantiTuru)
    {
        // For "Yönetim Kurulu" (Board) meetings: only board members
        // For "Kurucular Heyeti" (Founders) meetings: board members + founders
        // For all other meetings: students
        
        if (toplantiTuru == "Yönetim Kurulu")
        {
            // Only board members
            await CreateMemberAttendanceRecordsAsync(meetingId, membersQuery => 
                membersQuery.Where(m => m.IsYonetimKurulu && m.AktifMi));
        }
        else if (toplantiTuru == "Kurucular Heyeti")
        {
            // Board members + founders
            await CreateMemberAttendanceRecordsAsync(meetingId, membersQuery => 
                membersQuery.Where(m => (m.IsYonetimKurulu || m.IsMutevelli) && m.AktifMi));
        }
        else
        {
            // Students for other meeting types
            await CreateStudentAttendanceRecordsAsync(meetingId);
        }
    }

    private async Task CreateMemberAttendanceRecordsAsync(int meetingId, Func<IQueryable<Member>, IQueryable<Member>> filter)
    {
        var existingMemberIds = await _context.MemberMeetingAttendances
            .Where(a => a.MeetingId == meetingId)
            .Select(a => a.MemberId)
            .ToListAsync();

        var membersQuery = _context.Members.AsQueryable();
        var members = await filter(membersQuery)
            .Where(m => !existingMemberIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync();

        if (!members.Any())
            return;

        var attendances = members.Select(memberId => new MemberMeetingAttendance
        {
            MeetingId = meetingId,
            MemberId = memberId,
            Katildi = true
        });

        _context.MemberMeetingAttendances.AddRange(attendances);
        await _context.SaveChangesAsync();
    }

    private async Task CreateStudentAttendanceRecordsAsync(int meetingId)
    {
        var existingStudentIds = await _context.StudentMeetingAttendances
            .Where(a => a.MeetingId == meetingId)
            .Select(a => a.StudentId)
            .ToListAsync();

        var students = await _context.Students
            .Where(s => !existingStudentIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (!students.Any())
            return;

        var attendances = students.Select(studentId => new StudentMeetingAttendance
        {
            MeetingId = meetingId,
            StudentId = studentId,
            Katildi = true
        });

        _context.StudentMeetingAttendances.AddRange(attendances);
        await _context.SaveChangesAsync();
    }

    public async Task<int> ApplyLatestMeetingAbsencePenaltyAsync()
    {
        var result = await ApplyLatestMeetingAbsencePenaltyWithDetailsAsync();
        return result.Count;
    }

    public async Task<List<Student>> ApplyLatestMeetingAbsencePenaltyWithDetailsAsync()
    {
        var latestMeeting = await _context.Meetings
            .OrderByDescending(m => m.Tarih)
            .FirstOrDefaultAsync();

        if (latestMeeting == null)
            return new List<Student>();

        var absentStudentIds = await _context.StudentMeetingAttendances
            .Where(a => a.MeetingId == latestMeeting.Id && !a.Katildi)
            .Select(a => a.StudentId)
            .ToListAsync();

        if (!absentStudentIds.Any())
            return new List<Student>();

        var affectedStudents = await _context.Students
            .Where(s => absentStudentIds.Contains(s.Id) && s.AktifBursMu)
            .ToListAsync();

        // Infer term from meeting date for month-based scholarship cutting
        var term = await _scholarshipStatusService.InferTermFromDateAsync(latestMeeting.Tarih);

        foreach (var student in affectedStudents)
        {
            student.AktifBursMu = false;
            student.ScholarshipCutReason = $"Toplantı kontrolü ({latestMeeting.Baslik} - {latestMeeting.Tarih:dd.MM.yyyy})";
            student.ScholarshipCutDate = DateTime.Now;

            var reason = "Toplantıya katılmama";
            if (string.IsNullOrWhiteSpace(student.Notlar))
            {
                student.Notlar = reason;
            }
            else if (!student.Notlar.Contains(reason))
            {
                student.Notlar = $"{student.Notlar}; {reason}";
            }

            // Cut the specific month's scholarship in StudentScholarshipStatus table
            if (term != null)
            {
                await _scholarshipStatusService.CutScholarshipByMeetingDateAsync(
                    student.Id, term.Id, latestMeeting.Tarih, $"Toplantıya Katılmadı ({latestMeeting.Baslik})");
            }

            await _logService.LogAsync(
                "BursKes",
                $"{student.AdSoyad} - Bursu kesildi: {reason}"
            );
        }

        await _context.SaveChangesAsync();
        return affectedStudents;
    }

    /// <summary>
    /// Gets the most recent meeting
    /// </summary>
    public async Task<Meeting?> GetLatestMeetingAsync()
    {
        return await _context.Meetings
            .OrderByDescending(m => m.Tarih)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets students who didn't attend a specific meeting
    /// </summary>
    public async Task<List<Student>> GetAbsentStudentsAsync(int meetingId)
    {
        var absentStudentIds = await _context.StudentMeetingAttendances
            .Where(a => a.MeetingId == meetingId && !a.Katildi)
            .Select(a => a.StudentId)
            .ToListAsync();

        if (!absentStudentIds.Any())
            return new List<Student>();

        return await _context.Students
            .Where(s => absentStudentIds.Contains(s.Id) && s.AktifBursMu)
            .ToListAsync();
    }
}
