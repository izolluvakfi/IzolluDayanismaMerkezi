using ClosedXML.Excel;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class ExcelService
{
    public byte[] GenerateStudentTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Öğrenciler");

        // Başlıklar
        var headers = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Cinsiyet", "Doğum Tarihi", "Köy",
            "Ebeveyn Adı", "Ebeveyn Telefon", "Adres", "Üniversite", "Bölüm",
            "Sınıf", "Referans", "Burs Başlangıç", "Dönem", "IBAN"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Örnek satır 1
        worksheet.Cell(2, 1).Value = "20001";  // Öğrenci sicil formatı: 2XXXX
        worksheet.Cell(2, 2).Value = "Ayşe Yılmaz";
        worksheet.Cell(2, 3).Value = "12345678901";
        worksheet.Cell(2, 4).Value = "ayse.yilmaz@email.com";
        worksheet.Cell(2, 5).Value = "0532 123 4567";
        worksheet.Cell(2, 6).Value = "Kadın";
        worksheet.Cell(2, 7).Value = "15.05.2003";
        worksheet.Cell(2, 8).Value = "Gözeli";
        worksheet.Cell(2, 9).Value = "Mehmet Yılmaz";
        worksheet.Cell(2, 10).Value = "0532 999 8888";
        worksheet.Cell(2, 11).Value = "Gözeli Köyü, Akçadağ/Malatya";
        worksheet.Cell(2, 12).Value = "Fırat Üniversitesi";
        worksheet.Cell(2, 13).Value = "Bilgisayar Mühendisliği";
        worksheet.Cell(2, 14).Value = "2";
        worksheet.Cell(2, 15).Value = "Ahmet Demir";
        worksheet.Cell(2, 16).Value = "01.09.2024";
        worksheet.Cell(2, 17).Value = "2024-2025";
        worksheet.Cell(2, 18).Value = "TR330006100519786457841326";

        // Örnek satır 2
        worksheet.Cell(3, 1).Value = "20002";
        worksheet.Cell(3, 2).Value = "Mehmet Kaya";
        worksheet.Cell(3, 3).Value = "98765432109";
        worksheet.Cell(3, 4).Value = "mehmet.kaya@email.com";
        worksheet.Cell(3, 5).Value = "0533 456 7890";
        worksheet.Cell(3, 6).Value = "Erkek";
        worksheet.Cell(3, 7).Value = "22.11.2002";
        worksheet.Cell(3, 8).Value = "İzollu";
        worksheet.Cell(3, 9).Value = "Ali Kaya";
        worksheet.Cell(3, 10).Value = "0544 111 2222";
        worksheet.Cell(3, 11).Value = "İzollu Köyü, Akçadağ/Malatya";
        worksheet.Cell(3, 12).Value = "İnönü Üniversitesi";
        worksheet.Cell(3, 13).Value = "Tıp Fakültesi";
        worksheet.Cell(3, 14).Value = "3";
        worksheet.Cell(3, 15).Value = "Fatma Öztürk";
        worksheet.Cell(3, 16).Value = "01.09.2023";
        worksheet.Cell(3, 17).Value = "2024-2025";
        worksheet.Cell(3, 18).Value = "TR440007200620897568952437";

        // Açıklama satırı
        worksheet.Cell(5, 1).Value = "NOT:";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 2).Value = "Sicil numarası 2 ile başlamalıdır (örn: 20001, 20002). Boş bırakılırsa otomatik atanır.";
        worksheet.Cell(6, 2).Value = "Cinsiyet: Erkek veya Kadın";
        worksheet.Cell(7, 2).Value = "Tarih formatı: GG.AA.YYYY (örn: 15.05.2003)";
        worksheet.Cell(8, 2).Value = "Dönem formatı: YYYY-YYYY (örn: 2024-2025)";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportStudents(List<Student> students)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Öğrenciler");

        // Başlıklar
        var headers = new[]
        {
            "Ad Soyad", "TC No", "Email", "Telefon", "Doğum Tarihi", "Yaş", "Cinsiyet", "Köy",
            "Ebeveyn Adı", "Ebeveyn Telefon", "Adres", "Üniversite", "Bölüm",
            "Sınıf", "Referans", "Burs Başlangıç", "Burs Bitiş", "Sicil Numarası",
            "IBAN", "Mezun", "Mezuniyet Tarihi", "Aktif Burs", "Transkript Notu"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Veri satırları
        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            int row = i + 2;

            worksheet.Cell(row, 1).Value = student.AdSoyad;
            worksheet.Cell(row, 2).Value = student.TCNo ?? "";
            worksheet.Cell(row, 3).Value = student.Email ?? "";
            worksheet.Cell(row, 4).Value = student.Telefon ?? "";
            worksheet.Cell(row, 5).Value = student.DogumTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 6).Value = student.Yas?.ToString() ?? "";
            worksheet.Cell(row, 7).Value = FormatGender(student.Cinsiyet);
            worksheet.Cell(row, 8).Value = student.Koy ?? "";
            worksheet.Cell(row, 9).Value = student.EbeveynAdi ?? "";
            worksheet.Cell(row, 10).Value = student.EbeveynTelefon ?? "";
            worksheet.Cell(row, 11).Value = student.Adres ?? "";
            worksheet.Cell(row, 12).Value = student.Universite ?? "";
            worksheet.Cell(row, 13).Value = student.Bolum ?? "";
            worksheet.Cell(row, 14).Value = student.Sinif;
            worksheet.Cell(row, 15).Value = student.Referans ?? "";
            worksheet.Cell(row, 16).Value = student.BursBaslangicTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 17).Value = student.BursBitisTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 18).Value = student.SicilNumarasi ?? "";
            worksheet.Cell(row, 19).Value = student.IBAN ?? "";
            worksheet.Cell(row, 20).Value = student.MezunMu ? "Evet" : "Hayır";
            worksheet.Cell(row, 21).Value = student.MezuniyetTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 22).Value = student.AktifBursMu ? "Evet" : "Hayır";
            worksheet.Cell(row, 23).Value = student.TranskriptNotu ?? "";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public List<Student> ImportStudents(Stream fileStream, string activePeriod)
    {
        var students = new List<Student>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        // Başlık kontrolü
        var expectedHeaders = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Cinsiyet", "Doğum Tarihi", "Köy",
            "Ebeveyn Adı", "Ebeveyn Telefon", "Adres", "Üniversite", "Bölüm",
            "Sınıf", "Referans", "Burs Başlangıç", "Dönem", "IBAN"
        };

        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            var cellValue = worksheet.Cell(1, i + 1).GetString();
            if (cellValue != expectedHeaders[i])
            {
                throw new Exception($"Hatalı başlık: '{cellValue}'. Beklenen: '{expectedHeaders[i]}'");
            }
        }

        // Veri okuma (2. satırdan başla, 1. satır başlık)
        var rows = worksheet.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            try
            {
                var student = new Student
                {
                    SicilNumarasi = row.Cell(1).GetString(),
                    AdSoyad = row.Cell(2).GetString(),
                    TCNo = row.Cell(3).GetString(),
                    Email = row.Cell(4).GetString(),
                    Telefon = row.Cell(5).GetString(),
                    Cinsiyet = row.Cell(6).GetString(),
                    DogumTarihi = ParseDate(row.Cell(7).GetString()),
                    Koy = row.Cell(8).GetString(),
                    EbeveynAdi = row.Cell(9).GetString(),
                    EbeveynTelefon = row.Cell(10).GetString(),
                    Adres = row.Cell(11).GetString(),
                    Universite = row.Cell(12).GetString(),
                    Bolum = row.Cell(13).GetString(),
                    Sinif = ParseInt(row.Cell(14).GetString(), 1),
                    Referans = row.Cell(15).GetString(),
                    BursBaslangicTarihi = ParseDate(row.Cell(16).GetString()),
                    IBAN = row.Cell(18).GetString(),
                    AktifBursMu = true,
                    OlusturmaTarihi = DateTime.Now
                };

                students.Add(student);
            }
            catch (Exception ex)
            {
                throw new Exception($"Satır {row.RowNumber()} işlenirken hata: {ex.Message}");
            }
        }

        return students;
    }

    private DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, out var date))
            return date;

        return null;
    }

    private int ParseInt(string value, int defaultValue = 0)
    {
        if (int.TryParse(value, out var result))
            return result;

        return defaultValue;
    }

    private decimal ParseDecimal(string value)
    {
        if (decimal.TryParse(value, out var result))
            return result;

        return 0;
    }

    public byte[] GenerateMemberTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Üyeler");

        // Başlıklar
        var headers = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Adres", 
            "Meslek", "Firma", "Doğum Tarihi", "Köy / Mahalle", "Üyelik Türü", 
            "Üyelik Başlangıç Tarihi", "Durum", "Not"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Örnek satır 1
        worksheet.Cell(2, 1).Value = "10001";  // Üye sicil formatı: 1XXXX
        worksheet.Cell(2, 2).Value = "Ali Veli";
        worksheet.Cell(2, 3).Value = "12345678901";
        worksheet.Cell(2, 4).Value = "ali.veli@email.com";
        worksheet.Cell(2, 5).Value = "0532 123 4567";
        worksheet.Cell(2, 6).Value = "Atatürk Mah. No:15, Kadıköy/İstanbul";
        worksheet.Cell(2, 7).Value = "Mühendis";
        worksheet.Cell(2, 8).Value = "ABC Teknoloji A.Ş.";
        worksheet.Cell(2, 9).Value = "15.05.1980";
        worksheet.Cell(2, 10).Value = "İzollu";
        worksheet.Cell(2, 11).Value = "Üye";
        worksheet.Cell(2, 12).Value = "01.01.2024";
        worksheet.Cell(2, 13).Value = "Aktif";
        worksheet.Cell(2, 14).Value = "";

        // Örnek satır 2 - Yönetim Kurulu Üyesi
        worksheet.Cell(3, 1).Value = "10002";
        worksheet.Cell(3, 2).Value = "Fatma Demir";
        worksheet.Cell(3, 3).Value = "98765432109";
        worksheet.Cell(3, 4).Value = "fatma.demir@email.com";
        worksheet.Cell(3, 5).Value = "0533 456 7890";
        worksheet.Cell(3, 6).Value = "Cumhuriyet Cad. No:25, Beşiktaş/İstanbul";
        worksheet.Cell(3, 7).Value = "Avukat";
        worksheet.Cell(3, 8).Value = "Demir Hukuk Bürosu";
        worksheet.Cell(3, 9).Value = "22.11.1975";
        worksheet.Cell(3, 10).Value = "Gözeli";
        worksheet.Cell(3, 11).Value = "Yönetim Kurulu Üyesi";
        worksheet.Cell(3, 12).Value = "01.06.2023";
        worksheet.Cell(3, 13).Value = "Aktif";
        worksheet.Cell(3, 14).Value = "Yönetim kurulu başkan yardımcısı";

        // Örnek satır 3 - Mütevelli
        worksheet.Cell(4, 1).Value = "10003";
        worksheet.Cell(4, 2).Value = "Ahmet Yıldız";
        worksheet.Cell(4, 3).Value = "45678912301";
        worksheet.Cell(4, 4).Value = "ahmet.yildiz@email.com";
        worksheet.Cell(4, 5).Value = "0544 789 0123";
        worksheet.Cell(4, 6).Value = "Bağdat Cad. No:100, Kadıköy/İstanbul";
        worksheet.Cell(4, 7).Value = "İş İnsanı";
        worksheet.Cell(4, 8).Value = "Yıldız Holding";
        worksheet.Cell(4, 9).Value = "10.03.1970";
        worksheet.Cell(4, 10).Value = "Çavuşlu";
        worksheet.Cell(4, 11).Value = "Mütevelli Heyeti Üyesi";
        worksheet.Cell(4, 12).Value = "15.09.2022";
        worksheet.Cell(4, 13).Value = "Aktif";
        worksheet.Cell(4, 14).Value = "Vakıf kurucusu";

        // Açıklama satırları
        worksheet.Cell(6, 1).Value = "NOT:";
        worksheet.Cell(6, 1).Style.Font.Bold = true;
        worksheet.Cell(6, 2).Value = "Sicil numarası 1 ile başlamalıdır (örn: 10001, 10002). Boş bırakılırsa otomatik atanır.";
        worksheet.Cell(7, 2).Value = "Tarih formatı: GG.AA.YYYY (örn: 15.05.1980)";
        worksheet.Cell(8, 2).Value = "Üyelik Türü: Üye, Yönetim Kurulu Üyesi, Mütevelli Heyeti Üyesi, Denetim Kurulu Üyesi";
        worksheet.Cell(9, 2).Value = "Durum: Aktif veya Pasif";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportMembers(List<Member> members)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Üyeler");

        // Başlıklar
        var headers = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Adres", 
            "Meslek", "Doğum Tarihi", "Yaş", "Köy / Mahalle", "Üyelik Türü", 
            "Üyelik Başlangıç Tarihi", "Durum", "Yönetim Kurulu", "Mütevelli Heyeti",
            "Burs Veriyor", "Not"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Veri satırları
        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            int row = i + 2;

            worksheet.Cell(row, 1).Value = member.SicilNumarasi ?? "";
            worksheet.Cell(row, 2).Value = member.AdSoyad;
            worksheet.Cell(row, 3).Value = member.TCNo ?? "";
            worksheet.Cell(row, 4).Value = member.Email ?? "";
            worksheet.Cell(row, 5).Value = member.Telefon ?? "";
            worksheet.Cell(row, 6).Value = member.Adres ?? "";
            worksheet.Cell(row, 7).Value = member.Meslek ?? "";
            worksheet.Cell(row, 8).Value = member.DogumTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 9).Value = member.Yas?.ToString() ?? "";
            worksheet.Cell(row, 10).Value = member.Koy ?? "";
            worksheet.Cell(row, 11).Value = member.UyelikTuru ?? "";
            worksheet.Cell(row, 12).Value = member.UyelikBaslangicTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 13).Value = member.Durum ?? "";
            worksheet.Cell(row, 14).Value = member.IsYonetimKurulu ? "Evet" : "Hayır";
            worksheet.Cell(row, 15).Value = member.IsMutevelli ? "Evet" : "Hayır";
            worksheet.Cell(row, 16).Value = member.BursVeriyor ? "Evet" : "Hayır";
            worksheet.Cell(row, 17).Value = member.Notlar ?? "";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public List<Member> ImportMembers(Stream fileStream)
    {
        var members = new List<Member>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        // Başlık kontrolü
        var expectedHeaders = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Adres", 
            "Meslek", "Firma", "Doğum Tarihi", "Köy / Mahalle", "Üyelik Türü", 
            "Üyelik Başlangıç Tarihi", "Durum", "Not"
        };

        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            var cellValue = worksheet.Cell(1, i + 1).GetString();
            if (cellValue != expectedHeaders[i])
            {
                throw new Exception($"Hatalı başlık: '{cellValue}'. Beklenen: '{expectedHeaders[i]}'");
            }
        }

        // Veri okuma (2. satırdan başla, 1. satır başlık)
        var rows = worksheet.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            try
            {
                var member = new Member
                {
                    SicilNumarasi = row.Cell(1).GetString(),
                    AdSoyad = row.Cell(2).GetString(),
                    TCNo = row.Cell(3).GetString(),
                    Email = row.Cell(4).GetString(),
                    Telefon = row.Cell(5).GetString(),
                    Adres = row.Cell(6).GetString(),
                    Meslek = row.Cell(7).GetString(),
                    Firma = row.Cell(8).GetString(),
                    DogumTarihi = ParseDate(row.Cell(9).GetString()),
                    Koy = row.Cell(10).GetString(),
                    UyelikTuru = row.Cell(11).GetString(),
                    UyelikBaslangicTarihi = ParseDate(row.Cell(12).GetString()),
                    Durum = row.Cell(13).GetString(),
                    Notlar = row.Cell(14).GetString()
                };

                // Üyelik türüne göre otomatik bayrak ayarla
                var uyeType = member.UyelikTuru?.ToLower() ?? "";
                if (uyeType.Contains("yönetim kurulu"))
                    member.IsYonetimKurulu = true;
                if (uyeType.Contains("mütevelli"))
                    member.IsMutevelli = true;

                members.Add(member);
            }
            catch (Exception ex)
            {
                throw new Exception($"Satır {row.RowNumber()} işlenirken hata: {ex.Message}");
            }
        }

        return members;
    }

    public byte[] ExportScholarshipCutStudents(List<Student> students, string cutType)
    {
        using var workbook = new XLWorkbook();
        // Sheet name must be <= 31 characters
        var worksheet = workbook.Worksheets.Add("Bursu Kesilenler");

        // Başlıklar
        var headers = new[]
        {
            "Ad Soyad", "TC No", "Telefon", "Email", "Üniversite", "Bölüm", 
            "Sınıf", "Bağışçı", "Kesim Sebebi", "Kesim Tarihi", "Aylık Tutar", "Notlar"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Red;
            worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        // Veri satırları
        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            int row = i + 2;

            worksheet.Cell(row, 1).Value = student.AdSoyad;
            worksheet.Cell(row, 2).Value = student.TCNo ?? "";
            worksheet.Cell(row, 3).Value = student.Telefon ?? "";
            worksheet.Cell(row, 4).Value = student.Email ?? "";
            worksheet.Cell(row, 5).Value = student.Universite ?? "";
            worksheet.Cell(row, 6).Value = student.Bolum ?? "";
            worksheet.Cell(row, 7).Value = student.Sinif?.ToString() ?? "";
            worksheet.Cell(row, 8).Value = student.BagisciAdi ?? "";
            worksheet.Cell(row, 9).Value = student.ScholarshipCutReason ?? "";
            worksheet.Cell(row, 10).Value = student.ScholarshipCutDate?.ToString("dd.MM.yyyy HH:mm") ?? "";
            worksheet.Cell(row, 11).Value = student.AylikTutar;
            worksheet.Cell(row, 12).Value = student.Notlar ?? "";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ========== MEZUN ÖĞRENCİ METODLARI ==========

    public byte[] GenerateGraduatedStudentTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Mezun Öğrenciler");

        // Başlıklar
        var headers = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Cinsiyet", "Doğum Tarihi",
            "Köy", "Adres", "Üniversite", "Bölüm", "Meslek", "Firma",
            "Mezuniyet Tarihi", "İzollulu", "Notlar"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        // Örnek satır 1
        worksheet.Cell(2, 1).Value = "20001";  // Öğrenci sicil formatı: 2XXXX
        worksheet.Cell(2, 2).Value = "Ayşe Yılmaz";
        worksheet.Cell(2, 3).Value = "12345678901";
        worksheet.Cell(2, 4).Value = "ayse.yilmaz@email.com";
        worksheet.Cell(2, 5).Value = "0532 123 4567";
        worksheet.Cell(2, 6).Value = "Kadın";
        worksheet.Cell(2, 7).Value = "15.05.1998";
        worksheet.Cell(2, 8).Value = "Gözeli";
        worksheet.Cell(2, 9).Value = "Çankaya, Ankara";
        worksheet.Cell(2, 10).Value = "Fırat Üniversitesi";
        worksheet.Cell(2, 11).Value = "Bilgisayar Mühendisliği";
        worksheet.Cell(2, 12).Value = "Yazılım Geliştirici";
        worksheet.Cell(2, 13).Value = "ABC Teknoloji A.Ş.";
        worksheet.Cell(2, 14).Value = "01.07.2023";
        worksheet.Cell(2, 15).Value = "Evet";
        worksheet.Cell(2, 16).Value = "2019 yılından itibaren burs aldı";

        // Örnek satır 2
        worksheet.Cell(3, 1).Value = "20002";
        worksheet.Cell(3, 2).Value = "Mehmet Kaya";
        worksheet.Cell(3, 3).Value = "98765432109";
        worksheet.Cell(3, 4).Value = "mehmet.kaya@email.com";
        worksheet.Cell(3, 5).Value = "0533 456 7890";
        worksheet.Cell(3, 6).Value = "Erkek";
        worksheet.Cell(3, 7).Value = "22.11.1995";
        worksheet.Cell(3, 8).Value = "İzollu";
        worksheet.Cell(3, 9).Value = "Kadıköy, İstanbul";
        worksheet.Cell(3, 10).Value = "İnönü Üniversitesi";
        worksheet.Cell(3, 11).Value = "Tıp Fakültesi";
        worksheet.Cell(3, 12).Value = "Doktor";
        worksheet.Cell(3, 13).Value = "Devlet Hastanesi";
        worksheet.Cell(3, 14).Value = "15.06.2022";
        worksheet.Cell(3, 15).Value = "Evet";
        worksheet.Cell(3, 16).Value = "";

        // Açıklama satırları
        worksheet.Cell(5, 1).Value = "NOT:";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 2).Value = "Sicil numarası 2 ile başlamalıdır (örn: 20001, 20002). Boş bırakılırsa otomatik atanır.";
        worksheet.Cell(6, 2).Value = "Cinsiyet: Erkek veya Kadın";
        worksheet.Cell(7, 2).Value = "Tarih formatı: GG.AA.YYYY (örn: 15.05.1998)";
        worksheet.Cell(8, 2).Value = "İzollulu: Evet veya Hayır";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportGraduatedStudents(List<Student> students)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Mezun Öğrenciler");

        // Başlıklar
        var headers = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Cinsiyet", "Doğum Tarihi", "Yaş",
            "Köy", "Adres", "Üniversite", "Bölüm", "Meslek", "Firma",
            "Mezuniyet Tarihi", "İzollulu", "Notlar"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        // Veri satırları
        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            int row = i + 2;

            worksheet.Cell(row, 1).Value = student.SicilNumarasi ?? "";
            worksheet.Cell(row, 2).Value = student.AdSoyad;
            worksheet.Cell(row, 3).Value = student.TCNo ?? "";
            worksheet.Cell(row, 4).Value = student.Email ?? "";
            worksheet.Cell(row, 5).Value = student.Telefon ?? "";
            worksheet.Cell(row, 6).Value = FormatGender(student.Cinsiyet);
            worksheet.Cell(row, 7).Value = student.DogumTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 8).Value = student.Yas?.ToString() ?? "";
            worksheet.Cell(row, 9).Value = student.Koy ?? "";
            worksheet.Cell(row, 10).Value = student.Adres ?? "";
            worksheet.Cell(row, 11).Value = student.Universite ?? "";
            worksheet.Cell(row, 12).Value = student.Bolum ?? "";
            worksheet.Cell(row, 13).Value = student.Meslek ?? "";
            worksheet.Cell(row, 14).Value = student.Firma ?? "";
            worksheet.Cell(row, 15).Value = student.MezuniyetTarihi?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 16).Value = student.IsIzollulu ? "Evet" : "Hayır";
            worksheet.Cell(row, 17).Value = student.Notlar ?? "";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public List<Student> ImportGraduatedStudents(Stream fileStream)
    {
        var students = new List<Student>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        // Başlık kontrolü
        var expectedHeaders = new[]
        {
            "Sicil Numarası", "Ad Soyad", "TC No", "Email", "Telefon", "Cinsiyet", "Doğum Tarihi",
            "Köy", "Adres", "Üniversite", "Bölüm", "Meslek", "Firma",
            "Mezuniyet Tarihi", "İzollulu", "Notlar"
        };

        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            var cellValue = worksheet.Cell(1, i + 1).GetString();
            if (cellValue != expectedHeaders[i])
            {
                throw new Exception($"Hatalı başlık: '{cellValue}'. Beklenen: '{expectedHeaders[i]}'");
            }
        }

        // Veri okuma (2. satırdan başla, 1. satır başlık)
        var rows = worksheet.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            try
            {
                var adSoyad = row.Cell(2).GetString();
                if (string.IsNullOrWhiteSpace(adSoyad)) continue; // Boş satırları atla

                var student = new Student
                {
                    SicilNumarasi = row.Cell(1).GetString(),
                    AdSoyad = adSoyad,
                    TCNo = row.Cell(3).GetString(),
                    Email = row.Cell(4).GetString(),
                    Telefon = row.Cell(5).GetString(),
                    Cinsiyet = row.Cell(6).GetString(),
                    DogumTarihi = ParseDate(row.Cell(7).GetString()),
                    Koy = row.Cell(8).GetString(),
                    Adres = row.Cell(9).GetString(),
                    Universite = row.Cell(10).GetString(),
                    Bolum = row.Cell(11).GetString(),
                    Meslek = row.Cell(12).GetString(),
                    Firma = row.Cell(13).GetString(),
                    MezuniyetTarihi = ParseDate(row.Cell(14).GetString()),
                    IsIzollulu = row.Cell(15).GetString()?.ToLower() == "evet",
                    Notlar = row.Cell(16).GetString(),
                    MezunMu = true,
                    AktifBursMu = false,
                    OlusturmaTarihi = DateTime.Now
                };

                students.Add(student);
            }
            catch (Exception ex)
            {
                throw new Exception($"Satır {row.RowNumber()} işlenirken hata: {ex.Message}");
            }
        }

        return students;
    }

    private static string FormatGender(string? cinsiyet)
    {
        if (string.IsNullOrEmpty(cinsiyet))
            return "";
        
        var lower = cinsiyet.ToLower();
        if (lower == "erkek")
            return "Erkek";
        if (lower == "kadın" || lower == "kadin")
            return "Kadın";
        
        return cinsiyet;
    }
}