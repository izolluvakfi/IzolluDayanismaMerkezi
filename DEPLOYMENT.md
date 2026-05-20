# IzolluDayanismaMerkezi — Deployment Rehberi

## Neden Vercel Değil?

Bu uygulama **Blazor Server** (.NET 8) — server-side bir uygulama olduğundan Vercel'de çalışmaz.  
Vercel yalnızca statik siteler ve Node.js/Python/Go serverless fonksiyonları destekler.

---

## Seçenek 1: Railway (Önerilen — En Kolay)

Railway, Docker ile .NET uygulamalarını ücretsiz (500 saat/ay) çalıştırır ve kalıcı volume desteği vardır.

### Adımlar

1. **railway.app** → Sign up (GitHub ile)
2. **New Project** → **Deploy from GitHub repo** → `IzolluDayanismaMerkezi`
3. Settings → **Root Directory**: `IzolluCRM/IzolluDayanismaMerkezi`
4. **Add Volume** → Mount path: `/app/data`
5. **Variables** sekmesine ekle:
   ```
   ConnectionStrings__DefaultConnection = Data Source=/app/data/izolluvakfi.db
   ASPNETCORE_URLS = http://+:5000
   PORT = 5000
   ```
6. Deploy et — Railway otomatik Dockerfile'ı bulur ve build eder.
7. **Settings → Networking** → Generate Domain → URL'iniz hazır.

### Mevcut DB'yi Aktarma

Mevcut `izolluvakfi.db` dosyasını Volume'a atmak için Railway CLI kullanın:
```bash
# Railway CLI kur
npm install -g @railway/cli
railway login
railway run --service izollu-crm -- cp /local/izolluvakfi.db /app/data/izolluvakfi.db
```
Veya Railway dashboard'dan **Files** sekmesiyle manuel upload edin.

---

## Seçenek 2: Fly.io (Ücretsiz Tier — Güçlü)

```bash
# Fly CLI kur
winget install flyctl

# Login
flyctl auth login

# IzolluCRM/IzolluDayanismaMerkezi klasöründe:
cd D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi
flyctl launch --name izollu-crm --region ams

# Kalıcı volume oluştur (DB için)
flyctl volumes create izollu_data --size 1 --region ams

# fly.toml dosyasına ekle:
# [mounts]
#   source = "izollu_data"
#   destination = "/app/data"

# Deploy
flyctl deploy

# Environment variable ekle
flyctl secrets set ConnectionStrings__DefaultConnection="Data Source=/app/data/izolluvakfi.db"
```

---

## Seçenek 3: Render.com

1. render.com → New → Web Service → GitHub repo
2. **Root Directory**: `IzolluCRM/IzolluDayanismaMerkezi`
3. **Runtime**: Docker
4. **Disk** ekle → Mount path: `/app/data`, Size: 1 GB
5. Environment Variables:
   ```
   ConnectionStrings__DefaultConnection=Data Source=/app/data/izolluvakfi.db
   ASPNETCORE_URLS=http://+:5000
   PORT=5000
   ```

---

## Lokal Docker Test

```powershell
cd D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi

# Build ve çalıştır
docker-compose up --build

# Tarayıcıda aç
start http://localhost:5000
```

---

## Önemli Notlar

- **SQLite + Cloud**: SQLite dosya tabanlıdır, kalıcı volume olmadan her deploy'da veri sıfırlanır.  
  Yukarıdaki tüm seçenekler volume mount içerir — bunu mutlaka yapılandırın.
- **Mevcut veri**: İlk deploy'dan önce mevcut `izolluvakfi.db`'yi volume'a kopyalayın.
- **HTTPS**: Railway ve Render otomatik SSL sertifikası sağlar.
