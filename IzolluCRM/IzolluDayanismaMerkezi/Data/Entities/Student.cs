using System.ComponentModel.DataAnnotations;

namespace IzolluVakfi.Data.Entities;

public class Student
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Meslek { get; set; }

    [StringLength(200)]
    public string? Firma { get; set; }

    [StringLength(200)]
    public string? Universite { get; set; }

    public bool IsMalatyaUniversity { get; set; }

    [StringLength(20)]
    public string? Cinsiyet { get; set; }

    public int? Yas { get; set; }

    [StringLength(500)]
    public string? Adres { get; set; }

    [StringLength(50)]
    public string? SicilNumarasi { get; set; }

    [StringLength(11)]
    public string? TCNo { get; set; }

    public DateTime? DogumTarihi { get; set; }

    public bool IsIzollulu { get; set; }

    [StringLength(100)]
    public string? Koy { get; set; }

    [StringLength(100)]
    public string? EbeveynAdi { get; set; }

    [StringLength(20)]
    public string? EbeveynTelefon { get; set; }

    [StringLength(200)]
    public string? Bolum { get; set; }

    public int? Sinif { get; set; }

    [StringLength(200)]
    public string? Referans { get; set; }

    [StringLength(200)]
    public string? BagisciAdi { get; set; }

    public DateTime? BursBaslangicTarihi { get; set; }

    public DateTime? BursBitisTarihi { get; set; }

    public bool AktifBursMu { get; set; }

    public decimal AylikTutar { get; set; }

    public decimal ToplamAlinanBurs { get; set; }

    public bool MezunMu { get; set; }

    public DateTime? MezuniyetTarihi { get; set; }

    [StringLength(20)]
    public string? Telefon { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Notlar { get; set; }

    [StringLength(500)]
    public string? TranskriptNotu { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    [StringLength(34)]
    public string? IBAN { get; set; }

    [StringLength(100)]
    public string? ScholarshipCutReason { get; set; }

    public DateTime? ScholarshipCutDate { get; set; }

    public bool IsMaxGradeReached { get; set; }

    // Navigation properties
    public virtual ICollection<TranscriptRecord> Transcripts { get; set; } = new List<TranscriptRecord>();
    public virtual ICollection<StudentMeetingAttendance> MeetingAttendances { get; set; } = new List<StudentMeetingAttendance>();
    public virtual ICollection<ScholarshipPayment> ScholarshipPayments { get; set; } = new List<ScholarshipPayment>();
}
