using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IzolluVakfi.Services;

/// <summary>
/// Service for managing academic/financial terms and creating term-based snapshots.
/// </summary>
public class TermService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TermService> _logger;

    public TermService(ApplicationDbContext context, ILogger<TermService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the currently active term ID.
    /// </summary>
    /// <returns>The ID of the active term, or null if no active term exists.</returns>
    public async Task<int?> GetActiveTermIdAsync()
    {
        var activeTerm = await _context.Terms
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        return activeTerm == 0 ? null : activeTerm;
    }

    /// <summary>
    /// Gets the currently active term.
    /// </summary>
    /// <returns>The active term entity, or null if no active term exists.</returns>
    public async Task<Term?> GetActiveTermAsync()
    {
        return await _context.Terms
            .Where(t => t.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets all terms ordered by start date descending.
    /// </summary>
    /// <returns>List of all terms.</returns>
    public async Task<List<Term>> GetAllTermsAsync()
    {
        return await _context.Terms
            .OrderByDescending(t => t.Start)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific term by ID.
    /// </summary>
    public async Task<Term?> GetTermByIdAsync(int termId)
    {
        return await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == termId);
    }

    /// <summary>
    /// Opens a new academic/financial term.
    /// This method:
    /// 1. Validates the date range
    /// 2. Deactivates the previous active term
    /// 3. Creates a new term
    /// 4. Creates a default TermScholarshipConfig (or copies from previous term)
    /// 
    /// NOTE: Students and Members are term-independent. Scholarship tracking happens through
    /// ScholarshipPayment entities which link Student + Member + Term.
    /// </summary>
    /// <param name="start">Start date of the new term</param>
    /// <param name="end">End date of the new term</param>
    /// <param name="description">Optional description for the term</param>
    /// <returns>The newly created term</returns>
    /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
    public async Task<Term> OpenNewTermAsync(DateTime start, DateTime end, string? description = null)
    {
        // Validation
        if (end <= start)
        {
            throw new InvalidOperationException("Term end date must be after start date.");
        }

        // CRITICAL: Check for duplicate start date (unique constraint)
        var existingTermWithSameStart = await _context.Terms
            .AnyAsync(t => t.Start.Date == start.Date);
        
        if (existingTermWithSameStart)
        {
            throw new InvalidOperationException("Bu başlangıç tarihine sahip bir dönem zaten mevcut.");
        }

        if (end.Year != start.Year + 1 && end.Year != start.Year)
        {
            _logger.LogWarning("Term spans unusual date range: {Start} to {End}", start, end);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Find and deactivate the previous active term
            var previousActiveTerm = await _context.Terms
                .Where(t => t.IsActive)
                .FirstOrDefaultAsync();

            if (previousActiveTerm != null)
            {
                _logger.LogInformation("Deactivating previous term: {TermName}", previousActiveTerm.DisplayName);
                previousActiveTerm.IsActive = false;
                previousActiveTerm.UpdatedAt = DateTime.UtcNow;
            }

            // Create the new term
            var newTerm = new Term
            {
                Start = start,
                End = end,
                DisplayName = $"{start.Year}-{end.Year}",
                IsActive = true,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Terms.Add(newTerm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new term: {TermName} (ID: {TermId})", newTerm.DisplayName, newTerm.Id);

            // Create or copy TermScholarshipConfig for the new term
            if (previousActiveTerm != null)
            {
                var previousConfig = await _context.TermScholarshipConfigs
                    .FirstOrDefaultAsync(c => c.TermId == previousActiveTerm.Id);
                
                if (previousConfig != null)
                {
                    var newConfig = new TermScholarshipConfig
                    {
                        TermId = newTerm.Id,
                        YearlyAmount = previousConfig.YearlyAmount,
                        MonthlyAmount = previousConfig.MonthlyAmount,
                        Notes = $"Copied from {previousActiveTerm.DisplayName}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TermScholarshipConfigs.Add(newConfig);
                    _logger.LogInformation("Copied scholarship config from previous term (Yearly: {Yearly}, Monthly: {Monthly})", 
                        previousConfig.YearlyAmount, previousConfig.MonthlyAmount);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully opened new term: {TermName}", newTerm.DisplayName);
            return newTerm;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to open new term");
            throw;
        }
    }

    /// <summary>
    /// Creates the first term with a default scholarship configuration.
    /// Students and Members are term-independent, so no snapshot initialization needed.
    /// </summary>
    public async Task<Term> InitializeFirstTermAsync(DateTime start, DateTime end, decimal yearlyAmount, decimal monthlyAmount, string? description = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if terms already exist
            var existingTerms = await _context.Terms.AnyAsync();
            if (existingTerms)
            {
                throw new InvalidOperationException("Terms already exist. Use OpenNewTermAsync to create additional terms.");
            }

            // Create the first term
            var firstTerm = new Term
            {
                Start = start,
                End = end,
                DisplayName = $"{start.Year}-{end.Year}",
                IsActive = true,
                Description = description ?? "Initial term",
                CreatedAt = DateTime.UtcNow
            };

            _context.Terms.Add(firstTerm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created first term: {TermName} (ID: {TermId})", firstTerm.DisplayName, firstTerm.Id);

            // Create default scholarship configuration
            var scholarshipConfig = new TermScholarshipConfig
            {
                TermId = firstTerm.Id,
                YearlyAmount = yearlyAmount,
                MonthlyAmount = monthlyAmount,
                Notes = "Initial configuration",
                CreatedAt = DateTime.UtcNow
            };

            _context.TermScholarshipConfigs.Add(scholarshipConfig);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully initialized first term (Yearly: {Yearly}, Monthly: {Monthly})", 
                yearlyAmount, monthlyAmount);
            return firstTerm;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to initialize first term");
            throw;
        }
    }

    /// <summary>
    /// Updates a term's details (not recommended after data has been added).
    /// </summary>
    public async Task<Term> UpdateTermAsync(int termId, DateTime? start = null, DateTime? end = null, string? displayName = null, string? description = null)
    {
        var term = await _context.Terms.FindAsync(termId);
        if (term == null)
        {
            throw new InvalidOperationException($"Term with ID {termId} not found.");
        }

        // Validate end date is after start date
        var newStart = start ?? term.Start;
        var newEnd = end ?? term.End;
        if (newEnd <= newStart)
        {
            throw new InvalidOperationException("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır.");
        }

        // Check for duplicate StartDate (excluding current term)
        if (start.HasValue)
        {
            var existingTermWithSameStart = await _context.Terms
                .AnyAsync(t => t.Id != termId && t.Start.Date == start.Value.Date);
            if (existingTermWithSameStart)
            {
                throw new InvalidOperationException("Bu başlangıç tarihine sahip bir dönem zaten mevcut.");
            }
        }

        if (start.HasValue)
            term.Start = start.Value;
        
        if (end.HasValue)
            term.End = end.Value;
        
        if (displayName != null)
            term.DisplayName = displayName;
        
        if (description != null)
            term.Description = description;

        term.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return term;
    }

    /// <summary>
    /// Sets a specific term as the active term (deactivating all others).
    /// </summary>
    public async Task SetActiveTermAsync(int termId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var term = await _context.Terms.FindAsync(termId);
            if (term == null)
            {
                throw new InvalidOperationException($"Term with ID {termId} not found.");
            }

            // Deactivate all terms
            var allTerms = await _context.Terms.ToListAsync();
            foreach (var t in allTerms)
            {
                t.IsActive = false;
                t.UpdatedAt = DateTime.UtcNow;
            }

            // Activate the selected term
            term.IsActive = true;
            term.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Set term {TermName} (ID: {TermId}) as active", term.DisplayName, termId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to set active term");
            throw;
        }
    }

    /// <summary>
    /// Deletes a term. Cannot delete the active term.
    /// Note: TermScholarshipConfig has cascade delete, so it will be removed automatically.
    /// ScholarshipPayments have Restrict, so deletion will fail if payments exist.
    /// </summary>
    public async Task DeleteTermAsync(int termId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var term = await _context.Terms.FindAsync(termId);
            if (term == null)
            {
                throw new InvalidOperationException($"Term with ID {termId} not found.");
            }

            // Check if this term is active (from SystemSettings, NOT Term.IsActive)
            var systemSettings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (systemSettings?.ActiveTermId == termId)
            {
                throw new InvalidOperationException("Cannot delete the active term. Please set another term as active first.");
            }

            // Check if there are any scholarship payments for this term
            var hasPayments = await _context.ScholarshipPayments.AnyAsync(sp => sp.TermId == termId);
            if (hasPayments)
            {
                throw new InvalidOperationException("Cannot delete term with existing scholarship payments. Remove payments first.");
            }

            // Delete the term itself
            _context.Terms.Remove(term);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Deleted term {TermName} (ID: {TermId}) and all associated data", term.DisplayName, termId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to delete term {TermId}", termId);
            throw;
        }
    }

    /// <summary>
    /// Ensures that database Terms exist for all periods in the given list.
    /// Creates missing terms with standard academic year dates (Oct 1 - Jun 1).
    /// </summary>
    /// <param name="periods">List of period strings like "2024-2025"</param>
    /// <returns>Updated list of all terms</returns>
    public async Task<List<Term>> EnsureTermsFromPeriodsAsync(List<string> periods)
    {
        var existingTerms = await _context.Terms.AsNoTracking().ToListAsync();
        var missingPeriods = periods.Where(p => !existingTerms.Any(t => t.DisplayName == p)).ToList();
        
        if (!missingPeriods.Any())
        {
            return await GetAllTermsAsync();
        }

        var sortedMissing = missingPeriods.OrderBy(p => p).ToList();
        bool noTermsExist = !existingTerms.Any();
        
        foreach (var period in sortedMissing)
        {
            var parts = period.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
            {
                // Check if already exists (race condition protection)
                if (await _context.Terms.AnyAsync(t => t.DisplayName == period))
                    continue;
                    
                var startDate = new DateTime(startYear, 10, 1);
                var endDate = new DateTime(endYear, 6, 1);
                
                // Only set latest as active if no terms existed before
                bool setActive = noTermsExist && period == sortedMissing.Last();
                
                var newTerm = new Term
                {
                    Start = startDate,
                    End = endDate,
                    DisplayName = period,
                    IsActive = setActive,
                    Description = "Auto-created from settings",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Terms.Add(newTerm);
                _logger.LogInformation("Created term {Period} from settings", period);
            }
        }
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save synced terms");
        }
        
        return await GetAllTermsAsync();
    }
}
