using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using IzolluVakfi.Data;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class SettingsService
{
    private readonly ApplicationDbContext _context;

    public SettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0)
    {
        var value = await GetValueAsync(key);
        if (decimal.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue = 0)
    {
        var value = await GetValueAsync(key);
        if (int.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    public async Task<List<string>> GetListValueAsync(string key)
    {
        var value = await GetValueAsync(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine($"⚠️  [SettingsService] Empty value for key: {key}");
            return new List<string>();
        }

        var result = ParseListValue(value);
        Console.WriteLine($"✓ [SettingsService] Loaded {result.Count} items for key: {key}");
        return result;
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            setting = new Settings
            {
                Key = key,
                Value = value,
                Description = description,
                OlusturmaTarihi = DateTime.Now
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.GuncellemeTarihi = DateTime.Now;
            if (!string.IsNullOrEmpty(description))
                setting.Description = description;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SetListValueAsync(string key, List<string> values, string? description = null)
    {
        var normalized = NormalizeList(values);
        var value = string.Join("|", normalized);
        await SetValueAsync(key, value, description);
    }

    public async Task<List<Settings>> GetAllAsync()
    {
        return await _context.Settings
            .AsNoTracking()
            .OrderBy(s => s.Key)
            .ToListAsync();
    }

    public async Task DeleteAsync(string key)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }

    // Varsayılan ayarları başlat
    public async Task InitializeDefaultSettingsAsync()
    {
        // Varsayılan burs tutarı
        var bursTutari = await GetValueAsync("DefaultBursTutari");
        if (bursTutari == null)
        {
            await SetValueAsync("DefaultBursTutari", "3000", "Varsayılan aylık burs tutarı (TL)");
        }

        // Varsayılan sektörler
        var sektorler = await GetValueAsync("Sektorler");
        if (sektorler == null)
        {
            var defaultSektorler = new List<string>
            {
                "Teknoloji",
                "Yazılım / Bilişim",
                "Telekomünikasyon",
                "Finans / Bankacılık",
                "Sigorta",
                "Gayrimenkul / İnşaat",
                "Enerji",
                "Petrol / Doğalgaz",
                "Otomotiv",
                "Lojistik / Taşımacılık",
                "Üretim / Sanayi",
                "Kimya / Endüstriyel Ürünler",
                "Tekstil / Hazır Giyim",
                "Gıda / İçme",
                "Tarım / Hayvancılık",
                "Sağlık / Medikal",
                "Eğitim",
                "Danışmanlık",
                "Turizm / Otelcilik",
                "Perakende / Mağazacılık",
                "E-Ticaret",
                "Medya / Reklam / PR",
                "Savunma Sanayi",
                "Hukuk / Avukatlık",
                "Muhasebe / Mali Müşavirlik",
                "KOBİ / Serbest Meslek"
            };
            await SetListValueAsync("Sektorler", defaultSektorler, "İş adamları için sektör listesi");
        }

        // Varsayılan bölümler
        var bolumler = await GetValueAsync("Bolumler");
        if (bolumler == null)
        {
            var defaultBolumler = new List<string>
            {
                "Bilgisayar Mühendisliği",
                "Yazılım Mühendisliği",
                "Elektrik-Elektronik Mühendisliği",
                "Endüstri Mühendisliği",
                "Makine Mühendisliği",
                "İnşaat Mühendisliği",
                "Mekatronik Mühendisliği",
                "Havacılık ve Uzay Mühendisliği",
                "Yapay Zekâ Mühendisliği",
                "Veri Bilimi Mühendisliği",
                "Gıda Mühendisliği",
                "Çevre Mühendisliği",
                "Jeoloji Mühendisliği",
                "Jeofizik Mühendisliği",
                "Biyomedikal Mühendisliği",
                "Kimya Mühendisliği",
                "Metalurji ve Malzeme Mühendisliği",
                "Uçak Mühendisliği",
                "Uzay Mühendisliği",
                "Enerji Sistemleri Mühendisliği",
                "Tıp",
                "Diş Hekimliği",
                "Eczacılık",
                "Hemşirelik",
                "Fizyoterapi ve Rehabilitasyon",
                "Beslenme ve Diyetetik",
                "Sağlık Yönetimi",
                "Odyoloji",
                "Ergoterapi",
                "Gerontoloji",
                "Ebelik",
                "Veterinerlik",
                "Fizik",
                "Kimya",
                "Biyoloji",
                "Matematik",
                "Moleküler Biyoloji ve Genetik",
                "İstatistik",
                "Astronomi ve Uzay Bilimleri",
                "Bilgisayar Bilimleri",
                "Aktüerya Bilimleri",
                "Psikoloji",
                "Sosyoloji",
                "Felsefe",
                "Tarih",
                "Türk Dili ve Edebiyatı",
                "Arkeoloji",
                "Antropoloji",
                "Coğrafya",
                "Siyaset Bilimi ve Kamu Yönetimi",
                "Uluslararası İlişkiler",
                "Hukuk",
                "İktisat",
                "İşletme",
                "Maliye",
                "Çalışma Ekonomisi ve Endüstri İlişkileri",
                "Kamu Yönetimi",
                "Sosyal Hizmet",
                "Okul Öncesi Öğretmenliği",
                "Sınıf Öğretmenliği",
                "Fen Bilgisi Öğretmenliği",
                "Matematik Öğretmenliği",
                "Türkçe Öğretmenliği",
                "İngilizce Öğretmenliği",
                "Rehberlik ve Psikolojik Danışmanlık",
                "Özel Eğitim Öğretmenliği",
                "Tarih Öğretmenliği",
                "Coğrafya Öğretmenliği",
                "Grafik Tasarım",
                "İç Mimarlık",
                "İç Mimarlık ve Çevre Tasarımı",
                "Mimarlık",
                "Endüstriyel Tasarım",
                "Moda ve Tekstil Tasarımı",
                "Sahne Sanatları",
                "Resim",
                "Heykel",
                "Fotoğraf",
                "Sinema ve Televizyon",
                "Yeni Medya",
                "Gazetecilik",
                "Halkla İlişkiler ve Reklamcılık",
                "Radyo, Televizyon ve Sinema",
                "İletişim Tasarımı",
                "İlahiyat",
                "İslami İlimler",
                "Turizm İşletmeciliği",
                "Gastronomi ve Mutfak Sanatları",
                "Rekreasyon",
                "Spor Bilimleri",
                "Spor Yöneticiliği",
                "Deniz Ulaştırma İşletme Mühendisliği",
                "Gemi Makineleri İşletme Mühendisliği",
                "Pilotaj",
                "Havacılık Yönetimi",
                "Sivil Hava Ulaştırma İşletmeciliği",
                "İngiliz Dili ve Edebiyatı",
                "Mütercim Tercümanlık",
                "Rus Dili ve Edebiyatı",
                "Fransız Dili ve Edebiyatı",
                "Alman Dili ve Edebiyatı",
                "Arap Dili ve Edebiyatı",
                "Çeviribilim"
            };
            await SetListValueAsync("Bolumler", defaultBolumler, "Öğrenciler için bölüm listesi");
        }

        // Varsayılan üniversiteler
        var universiteler = await GetValueAsync("Universiteler");
        if (universiteler == null)
        {
            var defaultUniversiteler = new List<string>
            {
                "Çukurova Üniversitesi",
                "Adana Alparslan Türkeş Bilim ve Teknoloji Üniversitesi",
                "Adıyaman Üniversitesi",
                "Afyonkarahisar Sağlık Bilimleri Üniversitesi",
                "Afyon Kocatepe Üniversitesi",
                "Ağrı İbrahim Çeçen Üniversitesi",
                "Aksaray Üniversitesi",
                "Amasya Üniversitesi",
                "Yüksek İhtisas Üniversitesi",
                "Ufuk Üniversitesi",
                "Türk Hava Kurumu Üniversitesi",
                "TOBB Ekonomi ve Teknoloji Üniversitesi",
                "TED Üniversitesi",
                "Polis Akademisi",
                "Ostim Teknik Üniversitesi",
                "Orta Doğu Teknik Üniversitesi",
                "Lokman Hekim Üniversitesi",
                "Jandarma ve Sahil Güvenlik Akademisi (Askerî)",
                "İhsan Doğramacı Bilkent Üniversitesi",
                "Hacettepe Üniversitesi",
                "Gazi Üniversitesi",
                "Çankaya Üniversitesi",
                "Başkent Üniversitesi",
                "Atılım Üniversitesi",
                "Ankara Yıldırım Beyazıt Üniversitesi",
                "Ankara Üniversitesi",
                "Ankara Sosyal Bilimler Üniversitesi",
                "Ankara Müzik ve Güzel Sanatlar Üniversitesi",
                "Ankara Medipol Üniversitesi",
                "Ankara Hacı Bayram Veli Üniversitesi",
                "Ankara Bilim Üniversitesi",
                "Antalya Bilim Üniversitesi",
                "Antalya Belek Üniversitesi",
                "Alanya Üniversitesi",
                "Alanya Alaaddin Keykubat Üniversitesi",
                "Akdeniz Üniversitesi",
                "Ardahan Üniversitesi",
                "Artvin Çoruh Üniversitesi",
                "Aydın Adnan Menderes Üniversitesi",
                "Bandırma Onyedi Eylül Üniversitesi",
                "Balıkesir Üniversitesi",
                "Bartın Üniversitesi",
                "Batman Üniversitesi",
                "Bayburt Üniversitesi",
                "Bilecik Şeyh Edebali Üniversitesi",
                "Bingöl Üniversitesi",
                "Bitlis Eren Üniversitesi",
                "Bolu Abant İzzet Baysal Üniversitesi",
                "Burdur Mehmet Akif Ersoy Üniversitesi",
                "Mudanya Üniversitesi",
                "Bursa Uludağ Üniversitesi",
                "Bursa Teknik Üniversitesi",
                "Çanakkale Onsekiz Mart Üniversitesi",
                "Çankırı Karatekin Üniversitesi",
                "Hitit Üniversitesi",
                "Pamukkale Üniversitesi",
                "Dicle Üniversitesi",
                "Düzce Üniversitesi",
                "Trakya Üniversitesi",
                "Fırat Üniversitesi",
                "Erzincan Binali Yıldırım Üniversitesi",
                "Erzurum Teknik Üniversitesi",
                "Atatürk Üniversitesi",
                "Eskişehir Teknik Üniversitesi",
                "Eskişehir Osmangazi Üniversitesi",
                "Anadolu Üniversitesi",
                "Sanko Üniversitesi",
                "Hasan Kalyoncu Üniversitesi",
                "Gaziantep Üniversitesi",
                "Gaziantep İslam Bilim ve Teknoloji Üniversitesi",
                "Giresun Üniversitesi",
                "Gümüşhane Üniversitesi",
                "Hakkari Üniversitesi",
                "İskenderun Teknik Üniversitesi",
                "Hatay Mustafa Kemal Üniversitesi",
                "Iğdır Üniversitesi",
                "Süleyman Demirel Üniversitesi",
                "Isparta Uygulamalı Bilimler Üniversitesi",
                "Yıldız Teknik Üniversitesi",
                "Yeditepe Üniversitesi",
                "Üsküdar Üniversitesi",
                "Türk-Japon Bilim ve Teknoloji Üniversitesi",
                "Türk-Alman Üniversitesi",
                "Sağlık Bilimleri Üniversitesi",
                "Sabancı Üniversitesi",
                "Piri Reis Üniversitesi",
                "Özyeğin Üniversitesi",
                "Mimar Sinan Güzel Sanatlar Üniversitesi",
                "Milli Savunma Üniversitesi (Askerî)",
                "MEF Üniversitesi",
                "Marmara Üniversitesi",
                "Maltepe Üniversitesi",
                "Koç Üniversitesi",
                "Kadir Has Üniversitesi",
                "İstinye Üniversitesi",
                "İstanbul Yeni Yüzyıl Üniversitesi",
                "İstanbul Üniversitesi-Cerrahpaşa",
                "İstanbul Üniversitesi",
                "İstanbul Topkapı Üniversitesi",
                "İstanbul Ticaret Üniversitesi",
                "İstanbul Teknik Üniversitesi",
                "İstanbul Sağlık ve Teknoloji Üniversitesi",
                "İstanbul Sabahattin Zaim Üniversitesi",
                "İstanbul Rumeli Üniversitesi",
                "İstanbul Okan Üniversitesi",
                "İstanbul Nişantaşı Üniversitesi",
                "İstanbul Medipol Üniversitesi",
                "İstanbul Medeniyet Üniversitesi",
                "İstanbul Kültür Üniversitesi",
                "İstanbul Kent Üniversitesi",
                "İstanbul Gelişim Üniversitesi",
                "İstanbul Gedik Üniversitesi",
                "İstanbul Galata Üniversitesi",
                "İstanbul Esenyurt Üniversitesi",
                "İstanbul Bilgi Üniversitesi",
                "İstanbul Beykent Üniversitesi",
                "İstanbul Aydın Üniversitesi",
                "İstanbul Atlas Üniversitesi",
                "İstanbul Arel Üniversitesi",
                "İstanbul 29 Mayıs Üniversitesi",
                "İbn Haldun Üniversitesi",
                "Işık Üniversitesi",
                "Haliç Üniversitesi",
                "Galatasaray Üniversitesi",
                "Fenerbahçe Üniversitesi",
                "Fatih Sultan Mehmet Üniversitesi",
                "Doğuş Üniversitesi",
                "Demiroğlu Bilim Üniversitesi",
                "Boğaziçi Üniversitesi",
                "Biruni Üniversitesi",
                "Bezmialem Vakıf Üniversitesi",
                "Beykoz Üniversitesi",
                "Bahçeşehir Üniversitesi",
                "Altınbaş Üniversitesi",
                "Acıbadem Üniversitesi",
                "Yaşar Üniversitesi",
                "İzmir Yüksek Teknoloji Enstitüsü",
                "İzmir Tınaztepe Üniversitesi",
                "İzmir Kâtip Çelebi Üniversitesi",
                "İzmir Ekonomi Üniversitesi",
                "İzmir Demokrasi Üniversitesi",
                "İzmir Bakırçay Üniversitesi",
                "Ege Üniversitesi",
                "Dokuz Eylül Üniversitesi",
                "Kahramanmaraş Sütçü İmam Üniversitesi",
                "Kahramanmaraş İstiklal Üniversitesi",
                "Karabük Üniversitesi",
                "Karamanoğlu Mehmetbey Üniversitesi",
                "Kafkas Üniversitesi",
                "Kastamonu Üniversitesi",
                "Nuh Naci Yazgan Üniversitesi",
                "Kayseri Üniversitesi",
                "Erciyes Üniversitesi",
                "Abdullah Gül Üniversitesi",
                "Kırıkkale Üniversitesi",
                "Kırklareli Üniversitesi",
                "Kırşehir Ahi Evran Üniversitesi",
                "Kilis 7 Aralık Üniversitesi",
                "Kocaeli Üniversitesi",
                "Kocaeli Sağlık ve Teknoloji Üniversitesi",
                "Gebze Teknik Üniversitesi",
                "Selçuk Üniversitesi",
                "Necmettin Erbakan Üniversitesi",
                "KTO Karatay Üniversitesi",
                "Konya Teknik Üniversitesi",
                "Konya Gıda ve Tarım Üniversitesi",
                "Kütahya Sağlık Bilimleri Üniversitesi",
                "Kütahya Dumlupınar Üniversitesi",
                "Malatya Turgut Özal Üniversitesi",
                "İnönü Üniversitesi",
                "Manisa Celal Bayar Üniversitesi",
                "Mardin Artuklu Üniversitesi",
                "Toros Üniversitesi",
                "Tarsus Üniversitesi",
                "Mersin Üniversitesi",
                "Çağ Üniversitesi",
                "Muğla Sıtkı Koçman Üniversitesi",
                "Muş Alparslan Üniversitesi",
                "Nevşehir Hacı Bektaş Veli Üniversitesi",
                "Kapadokya Üniversitesi",
                "Niğde Ömer Halisdemir Üniversitesi",
                "Ordu Üniversitesi",
                "Osmaniye Korkut Ata Üniversitesi",
                "Recep Tayyip Erdoğan Üniversitesi",
                "Sakarya Üniversitesi",
                "Sakarya Uygulamalı Bilimler Üniversitesi",
                "Samsun Üniversitesi",
                "Ondokuz Mayıs Üniversitesi",
                "Siirt Üniversitesi",
                "Sinop Üniversitesi",
                "Sivas Cumhuriyet Üniversitesi",
                "Sivas Bilim ve Teknoloji Üniversitesi",
                "Harran Üniversitesi",
                "Şırnak Üniversitesi",
                "Tekirdağ Namık Kemal Üniversitesi",
                "Tokat Gaziosmanpaşa Üniversitesi",
                "Trabzon Üniversitesi",
                "Karadeniz Teknik Üniversitesi",
                "Avrasya Üniversitesi",
                "Munzur Üniversitesi",
                "Uşak Üniversitesi",
                "Van Yüzüncü Yıl Üniversitesi",
                "Yalova Üniversitesi",
                "Yozgat Bozok Üniversitesi",
                "Zonguldak Bülent Ecevit Üniversitesi"
            };
            await SetListValueAsync("Universiteler", defaultUniversiteler, "Öğrenciler için üniversite listesi");
        }

    }

    private static List<string> ParseListValue(string rawValue)
    {
        rawValue = rawValue.Trim();

        if (rawValue.StartsWith("[") && rawValue.EndsWith("]"))
        {
            try
            {
                var jsonList = JsonSerializer.Deserialize<List<string>>(rawValue);
                if (jsonList != null)
                {
                    return NormalizeList(jsonList);
                }
            }
            catch (JsonException)
            {
                // Fallback to delimiter parsing below when JSON parsing fails.
            }
        }

        var separators = rawValue.Contains('|')
            ? new[] { '|' }
            : new[] { '\n', '\r', ';', ',' };

        var parts = rawValue.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return NormalizeList(parts);
    }

    private static List<string> NormalizeList(IEnumerable<string?> source)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in source)
        {
            var trimmed = item?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    public async Task<List<Data.Models.ScholarshipAmountModel>> GetScholarshipAmountsAsync()
    {
        var json = await GetValueAsync("ScholarshipAmountsByPeriod");
        if (string.IsNullOrWhiteSpace(json))
            return new List<Data.Models.ScholarshipAmountModel>();

        try
        {
            return JsonSerializer.Deserialize<List<Data.Models.ScholarshipAmountModel>>(json) 
                ?? new List<Data.Models.ScholarshipAmountModel>();
        }
        catch
        {
            return new List<Data.Models.ScholarshipAmountModel>();
        }
    }

    public async Task SetScholarshipAmountsAsync(List<Data.Models.ScholarshipAmountModel> amounts)
    {
        var json = JsonSerializer.Serialize(amounts);
        await SetValueAsync("ScholarshipAmountsByPeriod", json, "Dönem bazlı burs tutarları");
    }
    public async Task<List<Settings>> GetScholarshipTermSettingsAsync()
    {
        return await _context.Settings
            .AsNoTracking()
            .Where(s => s.Key == "Dönem")
            .OrderByDescending(s => s.Value)
            .ToListAsync();
    }
}
