#!/bin/sh
echo "[entrypoint] Starting..."
mkdir -p /app/data

if [ ! -f /app/data/izolluvakfi.db ]; then
    echo "[entrypoint] /app/data/izolluvakfi.db not found, copying seed DB..."
    if [ -f /app/seed/izolluvakfi.db ]; then
        cp /app/seed/izolluvakfi.db /app/data/izolluvakfi.db
        SEED_SIZE=$(stat -c%s /app/seed/izolluvakfi.db 2>/dev/null || stat -f%z /app/seed/izolluvakfi.db)
        TARGET_SIZE=$(stat -c%s /app/data/izolluvakfi.db 2>/dev/null || stat -f%z /app/data/izolluvakfi.db)
        echo "[entrypoint] Seed copied. Source: ${SEED_SIZE} bytes, Target: ${TARGET_SIZE} bytes"
    else
        echo "[entrypoint] WARNING: Seed DB not found at /app/seed/izolluvakfi.db! App will start with empty DB."
        ls -la /app/seed/ 2>/dev/null || echo "[entrypoint] /app/seed/ directory does not exist"
    fi
else
    EXISTING_SIZE=$(stat -c%s /app/data/izolluvakfi.db 2>/dev/null || stat -f%z /app/data/izolluvakfi.db)
    echo "[entrypoint] Existing DB found at /app/data/izolluvakfi.db (${EXISTING_SIZE} bytes), keeping it."
fi

echo "[entrypoint] Launching dotnet IzolluVakfi.dll..."
exec dotnet IzolluVakfi.dll
