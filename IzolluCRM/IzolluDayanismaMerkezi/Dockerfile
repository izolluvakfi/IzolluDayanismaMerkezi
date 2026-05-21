# ============================================================
# IzolluDayanismaMerkezi - Blazor Server (.NET 8)
# Multi-stage Docker build
# ============================================================

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasini kopyala ve bagimlilikları yukle
COPY ["IzolluVakfi.csproj", "./"]
RUN dotnet restore "IzolluVakfi.csproj"

# Kaynak kodun tamamini kopyala
COPY . .

# Yayinla (WinExe yerine Exe olarak Linux icin override et)
RUN dotnet publish "IzolluVakfi.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# SQLite ve PDF render icin gerekli sistem kutuphaneleri/fontlar
RUN apt-get update && apt-get install -y --no-install-recommends \
    libsqlite3-0 \
    fontconfig \
    fonts-dejavu-core \
    fonts-dejavu-extra \
    fonts-liberation \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*

# Publish ciktisini kopyala
COPY --from=build /app/publish .

# Seed DB: ilk deploy'da /data/'ya kopyalanir, sonrasinda volume'daki veri kullanilir
COPY izolluvakfi.db /app/seed/izolluvakfi.db
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Port
EXPOSE 8080
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["/app/entrypoint.sh"]
