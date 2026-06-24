using System.ComponentModel.DataAnnotations;

namespace IzolluVakfi.Data.Entities;

public class ErrorLog
{
    public int Id { get; set; }

    public DateTime Tarih { get; set; }

    [Required]
    [StringLength(100)]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Kaynak { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Mesaj { get; set; } = string.Empty;

    public string? Detay { get; set; }
}
