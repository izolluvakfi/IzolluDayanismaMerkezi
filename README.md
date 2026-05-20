# İzollu Dayanışma Merkezi — CRM

Blazor Server (.NET 8) tabanlı burs yönetim ve CRM uygulaması. Öğrenci takibi, bağışçı yönetimi, burs ödemeleri ve toplantı kayıtlarını kapsar.

## Teknoloji Stack

- **Framework:** ASP.NET Core 8 + Blazor Server
- **UI:** MudBlazor
- **Veritabanı:** SQLite (EF Core 8)
- **PDF:** QuestPDF
- **Excel:** ClosedXML

## Geliştirme Ortamı

### Gereksinimler

- .NET 8 SDK
- (Opsiyonel) VS Code veya Visual Studio 2022+

### Başlatma

```bash
cd IzolluCRM/IzolluDayanismaMerkezi
dotnet restore
dotnet run
```

Uygulama `http://localhost:5000` adresinde açılır. İlk çalıştırmada SQLite veritabanı otomatik oluşturulur.

---

## Deployment

### Mimari Seçenekler

Bu uygulama iki farklı yaklaşımla deploy edilebilir:

---

## 1. Monolith Deployment (Önerilen)

Tüm uygulama tek bir konteyner olarak çalışır. Küçük-orta ölçekli kullanım için idealdir.

```
┌─────────────────────────────────┐
│         Docker Container        │
│                                 │
│   Blazor Server (.NET 8)        │
│   ├── UI (Razor/MudBlazor)      │
│   ├── Business Logic            │
│   ├── EF Core                   │
│   └── SQLite DB                 │
│                                 │
│   Volume: /app/data/            │
└─────────────────────────────────┘
```

### Docker ile Lokal Test

```bash
cd IzolluCRM/IzolluDayanismaMerkezi

# Build ve çalıştır
docker-compose up --build

# Tarayıcıda aç
open http://localhost:5000
```

### Railway (Ücretsiz, Önerilen)

1. [railway.app](https://railway.app) → GitHub ile giriş
2. **New Project** → **Deploy from GitHub repo** → `IzolluDayanismaMerkezi`
3. **Settings → Root Directory:** `IzolluCRM/IzolluDayanismaMerkezi`
4. **Add Volume** → Mount path: `/app/data`
5. **Variables** ekle:
   ```
   ConnectionStrings__DefaultConnection=Data Source=/app/data/izolluvakfi.db
   ASPNETCORE_URLS=http://+:5000
   PORT=5000
   ```
6. Deploy → Railway otomatik Dockerfile'ı bulur ve build eder.

### Render.com

1. [render.com](https://render.com) → New → Web Service → GitHub repo
2. **Root Directory:** `IzolluCRM/IzolluDayanismaMerkezi`
3. **Runtime:** Docker
4. **Disk** ekle → Mount: `/app/data`, Boyut: 1 GB
5. Environment Variables:
   ```
   ConnectionStrings__DefaultConnection=Data Source=/app/data/izolluvakfi.db
   ASPNETCORE_URLS=http://+:5000
   ```

### Fly.io

```bash
# CLI kur
winget install flyctl   # Windows
# veya
brew install flyctl     # macOS

flyctl auth login
cd IzolluCRM/IzolluDayanismaMerkezi

# Uygulama oluştur
flyctl launch --name izollu-crm --region ams

# Kalıcı volume (SQLite için zorunlu)
flyctl volumes create izollu_data --size 1 --region ams

# fly.toml içine ekle:
# [mounts]
#   source = "izollu_data"
#   destination = "/app/data"

# Environment variable
flyctl secrets set ConnectionStrings__DefaultConnection="Data Source=/app/data/izolluvakfi.db"

# Deploy
flyctl deploy
```

### Mevcut Veriyi Taşıma

İlk deployment'tan önce mevcut `izolluvakfi.db` dosyasını volume'a kopyalayın:

```bash
# Railway CLI ile
railway run cp /local/izolluvakfi.db /app/data/izolluvakfi.db

# Fly.io ile
flyctl ssh sftp shell
put izolluvakfi.db /app/data/izolluvakfi.db
```

---

## 2. Microservice Deployment

Uygulamayı bağımsız servisler olarak bölmek için önerilen mimari:

```
                    ┌─────────────┐
                    │  API Gateway │
                    │  (nginx /    │
                    │   Traefik)   │
                    └──────┬──────┘
                           │
          ┌────────────────┼────────────────┐
          │                │                │
   ┌──────▼──────┐  ┌──────▼──────┐  ┌─────▼───────┐
   │  Blazor UI  │  │  Core API   │  │  Report API  │
   │  (Frontend) │  │  (.NET 8)   │  │  (PDF/Excel) │
   │  Port 5000  │  │  Port 5001  │  │  Port 5002   │
   └─────────────┘  └──────┬──────┘  └─────────────┘
                           │
                    ┌──────▼──────┐
                    │  PostgreSQL  │
                    │  (veya      │
                    │   SQLite)   │
                    └─────────────┘
```

### Servisler

| Servis | Sorumluluk | Port |
|--------|-----------|------|
| `blazor-ui` | Blazor Server UI, sayfa render | 5000 |
| `core-api` | Üye, öğrenci, burs CRUD API | 5001 |
| `report-api` | PDF/Excel rapor üretimi | 5002 |
| `db` | PostgreSQL veya SQLite | 5432 |

### Docker Compose (Microservice)

```yaml
version: '3.8'

services:
  blazor-ui:
    build:
      context: ./IzolluCRM/IzolluDayanismaMerkezi
    ports:
      - "5000:5000"
    environment:
      - ApiBaseUrl=http://core-api:5001
    depends_on:
      - core-api

  core-api:
    build:
      context: ./CoreApi   # ayrı proje
    ports:
      - "5001:5001"
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Database=izollu;Username=postgres;Password=secret
    depends_on:
      - db

  report-api:
    build:
      context: ./ReportApi   # ayrı proje
    ports:
      - "5002:5002"

  db:
    image: postgres:16-alpine
    volumes:
      - pgdata:/var/lib/postgresql/data
    environment:
      POSTGRES_DB: izollu
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: secret

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf
    depends_on:
      - blazor-ui

volumes:
  pgdata:
```

### Microservice için Kubernetes (İleri Seviye)

```bash
# Her servis için ayrı Deployment + Service
kubectl apply -f k8s/blazor-ui-deployment.yaml
kubectl apply -f k8s/core-api-deployment.yaml
kubectl apply -f k8s/report-api-deployment.yaml
kubectl apply -f k8s/postgres-statefulset.yaml
kubectl apply -f k8s/ingress.yaml
```

### Ne Zaman Microservice?

Monolith önerin şu koşullarda microservice'e geçin:
- 10+ eş zamanlı kullanıcı ve belirgin performans sorunu
- Rapor üretimi UI'ı bloke etmeye başladığında
- Farklı ekiplerin farklı servisleri bağımsız deploy etmesi gerektiğinde

Mevcut kullanım için **monolith + Railway** yeterlidir.

---

## Ortam Değişkenleri

| Değişken | Varsayılan | Açıklama |
|----------|-----------|---------|
| `ConnectionStrings__DefaultConnection` | `Data Source=izolluvakfi.db` | SQLite bağlantısı |
| `ASPNETCORE_URLS` | `http://+:5000` | Dinlenecek adres |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Ortam adı |

## Lisans

Bu proje İzollu Dayanışma Vakfı'na aittir.
