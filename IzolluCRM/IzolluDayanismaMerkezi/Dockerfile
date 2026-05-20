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
    /p:OutputType=Exe \
    -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# SQLite icin gerekli sistem kutuphaneleri
RUN apt-get update && apt-get install -y --no-install-recommends \
    libsqlite3-0 \
    && rm -rf /var/lib/apt/lists/*

# Publish ciktisini kopyala
COPY --from=build /app/publish .

# Veri dizini: SQLite DB icin kalici volume noktasi
# Railway/Render/Fly.io'da bu dizini volume olarak baglayin
RUN mkdir -p /app/data
ENV DATABASE_PATH=/app/data/izolluvakfi.db

# Port
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Baslangic
ENTRYPOINT ["dotnet", "IzolluVakfi.dll"]
