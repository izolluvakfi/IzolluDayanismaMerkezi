using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IzolluVakfi.Services;

public class MemberScholarshipCommitmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MemberScholarshipCommitmentService> _logger;

    public MemberScholarshipCommitmentService(
        ApplicationDbContext context,
        ILogger<MemberScholarshipCommitmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all commitments for a specific member
    /// </summary>
    public async Task<List<MemberScholarshipCommitment>> GetByMemberAsync(int memberId)
    {
        return await _context.MemberScholarshipCommitments
            .Include(c => c.Member)
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all commitments
    /// </summary>
    public async Task<List<MemberScholarshipCommitment>> GetAllAsync()
    {
        return await _context.MemberScholarshipCommitments
            .Include(c => c.Member)
            .OrderBy(c => c.Member.AdSoyad)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new commitment
    /// </summary>
    public async Task<MemberScholarshipCommitment> CreateAsync(MemberScholarshipCommitment commitment)
    {
        commitment.CreatedAt = DateTime.UtcNow;
        _context.MemberScholarshipCommitments.Add(commitment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created commitment: MemberId={MemberId}, PledgedCount={Count}",
            commitment.MemberId, commitment.PledgedCount);

        return commitment;
    }

    /// <summary>
    /// Update an existing commitment
    /// </summary>
    public async Task<MemberScholarshipCommitment> UpdateAsync(MemberScholarshipCommitment commitment)
    {
        var existing = await _context.MemberScholarshipCommitments.FindAsync(commitment.Id);
        if (existing == null)
        {
            throw new InvalidOperationException("Taahhüt kaydı bulunamadı.");
        }

        existing.PledgedCount = commitment.PledgedCount;
        existing.GivenCount = commitment.GivenCount;
        existing.YearlyAmountPerScholarship = commitment.YearlyAmountPerScholarship;
        existing.Notes = commitment.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated commitment: Id={Id}, PledgedCount={Pledged}, GivenCount={Given}",
            commitment.Id, commitment.PledgedCount, commitment.GivenCount);

        return existing;
    }

    /// <summary>
    /// Delete a commitment
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var commitment = await _context.MemberScholarshipCommitments.FindAsync(id);
        if (commitment != null)
        {
            _context.MemberScholarshipCommitments.Remove(commitment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted commitment: Id={Id}", id);
        }
    }

    /// <summary>
    /// Get total scholarship statistics
    /// </summary>
    public async Task<(int DonorCount, decimal TotalMonthly, decimal TotalYearly, int TotalPledged, int TotalGiven)> GetStatisticsAsync()
    {
        var commitments = await _context.MemberScholarshipCommitments
            .ToListAsync();

        if (!commitments.Any())
        {
            return (0, 0, 0, 0, 0);
        }

        var donorCount = commitments.Select(c => c.MemberId).Distinct().Count();
        var totalYearly = commitments.Sum(c => c.TotalYearlyAmount);
        var totalMonthly = commitments.Sum(c => c.TotalMonthlyAmount);
        var totalPledged = commitments.Sum(c => c.PledgedCount);
        var totalGiven = commitments.Sum(c => c.GivenCount);

        return (donorCount, totalMonthly, totalYearly, totalPledged, totalGiven);
    }

    /// <summary>
    /// Update YearlyAmountPerScholarship for all commitments in a specific period
    /// </summary>
    public async Task<int> UpdateAmountByPeriodAsync(string academicYear, decimal newYearlyAmount)
    {
        var commitments = await _context.MemberScholarshipCommitments
            .Where(c => c.AcademicYear == academicYear)
            .ToListAsync();

        if (!commitments.Any())
        {
            _logger.LogInformation("No commitments found for period {Period}", academicYear);
            return 0;
        }

        foreach (var commitment in commitments)
        {
            commitment.YearlyAmountPerScholarship = newYearlyAmount;
            commitment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated {Count} commitments for period {Period} to YearlyAmount={Amount}",
            commitments.Count, academicYear, newYearlyAmount);

        return commitments.Count;
    }
}
