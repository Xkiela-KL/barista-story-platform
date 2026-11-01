#!/usr/bin/env bash
# Veröffentlicht API und Website (portabel, läuft auf dem arm64-Pi), lädt sie auf den Pi und startet die Container neu.
#   deploy/deploy.sh xkiela@raspberrypi /opt/xkstories        (Git Bash unter Windows)
set -euo pipefail
HOST=${1:-xkiela@raspberrypi}
DIR=${2:-/opt/xkstories}
ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUT="$ROOT/artifacts/publish"

rm -rf "$OUT" && mkdir -p "$OUT"
for app in api web; do
  project=$([ "$app" = api ] && echo StoryPlatform.Api || echo StoryPlatform.Web)
  dotnet publish "$ROOT/src/$project/$project.csproj" -c Release \
    -p:UseAppHost=false -o "$OUT/$app"
done

ssh "$HOST" "mkdir -p '$DIR/app'"
scp "$ROOT/deploy/docker-compose.yml" "$ROOT/deploy/Dockerfile.prebuilt" "$HOST:$DIR/"
ssh "$HOST" "rm -rf '$DIR/app/api' '$DIR/app/web'"
scp -r "$OUT/api" "$OUT/web" "$HOST:$DIR/app/"
ssh "$HOST" "cd '$DIR' && test -f .env || { echo '.env fehlt in $DIR (Vorlage: deploy/.env.example)'; exit 1; }; docker compose up -d --build && docker compose ps"
echo "Fertig. Prüfen: curl -fsS http://127.0.0.1:8086/health und http://127.0.0.1:8087/health auf dem Pi"
