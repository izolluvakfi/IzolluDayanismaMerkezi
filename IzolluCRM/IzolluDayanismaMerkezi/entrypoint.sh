#!/bin/sh
mkdir -p /data
if [ ! -f /data/izolluvakfi.db ]; then
    echo "Seed DB copying to /data/..."
    cp /app/seed/izolluvakfi.db /data/izolluvakfi.db
    echo "Done."
fi
exec dotnet IzolluVakfi.dll
