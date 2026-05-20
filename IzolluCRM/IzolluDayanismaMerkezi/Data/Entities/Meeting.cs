using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IzolluVakfi.Data.Entities;

public class Meeting
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Baslik { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ToplantiTuru { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Konum { get; set; }

    [StringLength(1000)]
    public string? Aciklama { get; set; }

    public string? ToplantiNotlari { get; set; }

    public bool IsActive { get; set; } = false;

    public DateTime Tarih { get; set; }

    public DateTime BitisTarihi { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    // Term (Dönem) ilişkisi
    public int? TermId { get; set; }
    
    [ForeignKey("TermId")]
    public virtual Term? Term { get; set; }

    public virtual ICollection<StudentMeetingAttendance> Attendances { get; set; } = new List<StudentMeetingAttendance>();
    public virtual ICollection<MemberMeetingAttendance> MemberAttendances { get; set; } = new List<MemberMeetingAttendance>();
}
