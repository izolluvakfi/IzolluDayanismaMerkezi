#!/bin/sh
mkdir -p /app/data
if [ ! -f /app/data/izolluvakfi.db ]; then
    echo "Seed DB copying to /app/data/..."
    cp /app/seed/izolluvakfi.db /app/data/izolluvakfi.db
    echo "Done."
fi
exec dotnet IzolluVakfi.dll
