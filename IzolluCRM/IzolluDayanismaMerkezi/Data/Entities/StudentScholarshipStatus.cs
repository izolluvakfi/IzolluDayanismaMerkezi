using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IzolluVakfi.Data.Entities;

/// <summary>
/// Stores per-student, per-term, per-month scholarship status.
/// Tracks whether scholarship was paid/active for each month and reason if cut.
/// </summary>
public class StudentScholarshipStatus
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int StudentId { get; set; }
    
    [Required]
    public int TermId { get; set; }
    
    /// <summary>
    /// Month number (1-12). For scholarship tracking: 10=Oct, 11=Nov, 12=Dec, 1=Jan, 2=Feb, 3=Mar, 4=Apr, 5=May
    /// </summary>
    [Required]
    public int Month { get; set; }
    
    /// <summary>
    /// Year for this month (important for academic year spanning two calendar years)
    /// </summary>
    [Required]
    public int Year { get; set; }
    
    /// <summary>
    /// True = Paid/Active, False = Cut/Unpaid
    /// </summary>
    public bool IsPaid { get; set; } = true;
    
    /// <summary>
    /// Reason for cutting scholarship (if IsPaid = false)
    /// Examples: "Toplantıya Katılmadı", "Transkript GNO < 2.0", "Manuel Kesim"
    /// </summary>
    public string? CutReason { get; set; }
    
    /// <summary>
    /// Amount for this month (copied from student's AylikTutar at time of creation)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    // Navigation properties
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
    
    [ForeignKey("TermId")]
    public virtual Term? Term { get; set; }
    
    // Helper method to get Turkish month name
    public string GetMonthName()
    {
        return Month switch
        {
            1 => "Ocak",
            2 => "Şubat",
            3 => "Mart",
            4 => "Nisan",
            5 => "Mayıs",
            6 => "Haziran",
            7 => "Temmuz",
            8 => "Ağustos",
            9 => "Eylül",
            10 => "Ekim",
            11 => "Kasım",
            12 => "Aralık",
            _ => "Bilinmiyor"
        };
    }
    
    // Static helper to check if month is in scholarship period (Oct-May)
    public static bool IsScholarshipMonth(int month)
    {
        return month >= 10 || month <= 5; // Oct(10), Nov(11), Dec(12), Jan(1), Feb(2), Mar(3), Apr(4), May(5)
    }
    
    // Static helper to get ordered scholarship months for a term
    public static List<(int Month, int YearOffset)> GetScholarshipMonths()
    {
        return new List<(int Month, int YearOffset)>
        {
            (10, 0),  // Ekim - first year
            (11, 0),  // Kasım - first year
            (12, 0),  // Aralık - first year
            (1, 1),   // Ocak - second year
            (2, 1),   // Şubat - second year
            (3, 1),   // Mart - second year
            (4, 1),   // Nisan - second year
            (5, 1)    // Mayıs - second year
        };
    }
}
