#!/usr/bin/env bash
# XKStories auf dem Raspberry Pi einrichten oder aktualisieren. Wird von install-xkstories.ps1 aufgerufen:
#   bash ~/xkstories-upload/pi-setup.sh
# Erwartet in ~/xkstories-upload: docker-compose.yml, Dockerfile.prebuilt, .env.example, app/api, app/web
# und optional legal.env (Impressumsangaben, nur beim ersten Mal).
#
# Ablauf: 1. prüfen (nichts ändern) · 2. Container · 3. Tunnel-Regeln · 4. alle Hostnamen prüfen.
# Ändert sich durch die Tunnel-Regeln etwas an bestehenden Seiten, wird die alte Konfiguration zurückgespielt.
set -euo pipefail

UPLOAD="$HOME/xkstories-upload"
DIR=${XK_DIR:-/opt/xkstories}
CONFIG=${XK_CF_CONFIG:-/etc/cloudflared/config.yml}
SITE_HOST=tv.xkiela.dev
API_HOST=api.xkiela.dev
LOG="$UPLOAD/install.log"
STAMP=$(date +%Y%m%d%H%M%S)

exec > >(tee -a "$LOG") 2>&1
step() { echo; echo "== $*"; }
ok()   { echo "   OK   $*"; }
warn() { echo "   WARN $*"; }
fail() { echo "   FEHLER $*"; echo; echo "Abgebrochen. Protokoll: $LOG"; exit 1; }

echo "XKStories Installation $(date '+%d.%m.%Y %H:%M:%S') auf $(hostname)"

# ---------------------------------------------------------------- 1. Prüfen
step "1/4 Voraussetzungen prüfen (ändert nichts)"
[ -d "$UPLOAD/app/api" ] && [ -d "$UPLOAD/app/web" ] || fail "Upload unvollständig: $UPLOAD/app/api und app/web fehlen."
command -v docker >/dev/null || fail "Docker ist nicht installiert."
docker compose version >/dev/null 2>&1 || fail "'docker compose' fehlt."
docker info >/dev/null 2>&1 || fail "Docker läuft nicht oder $(id -un) ist nicht in der Gruppe docker."
ok "Docker $(docker version --format '{{.Server.Version}}')"
command -v curl >/dev/null || fail "curl fehlt (sudo apt install curl)."

if [ -f "$DIR/.env" ]; then
  # shellcheck disable=SC1090
  WEB_PORT=$(grep -E '^WEB_PORT=' "$DIR/.env" | cut -d= -f2 || true)
  API_PORT=$(grep -E '^API_PORT=' "$DIR/.env" | cut -d= -f2 || true)
fi
WEB_PORT=${WEB_PORT:-8086}
API_PORT=${API_PORT:-8087}

ours() { docker ps --filter "label=com.docker.compose.project=xkstories" --format '{{.Ports}}' | grep -q "127.0.0.1:$1->"; }
for port in "$WEB_PORT" "$API_PORT"; do
  if ss -Hltn "( sport = :$port )" | grep -q .; then
    ours "$port" && ok "Port $port gehört bereits zu XKStories" || fail "Port $port ist belegt: $(ss -Hltnp "( sport = :$port )" 2>/dev/null | head -1). WEB_PORT/API_PORT in $DIR/.env ändern."
  else
    ok "Port $port frei"
  fi
done

TUNNEL=""
if [ -f "$CONFIG" ]; then
  TUNNEL=$(awk '/^tunnel:/{print $2}' "$CONFIG")
  ok "Cloudflare Tunnel $TUNNEL ($CONFIG)"
  for h in "$SITE_HOST" "$API_HOST"; do
    if grep -qE "hostname:[[:space:]]*${h}[[:space:]]*$" "$CONFIG"; then
      svc=$(grep -A1 -E "hostname:[[:space:]]*$h" "$CONFIG" | awk '/service:/{print $NF}')
      case "$svc" in
        "http://localhost:$WEB_PORT"|"http://localhost:$API_PORT") ok "$h ist bereits eingerichtet ($svc)";;
        *) fail "$h zeigt im Tunnel schon auf $svc. Nichts geändert; bitte klären, ob der Name anders genutzt wird.";;
      esac
    fi
  done
  grep -qE '^[[:space:]]*-[[:space:]]*service:[[:space:]]*http_status:404' "$CONFIG" || fail "Keine Catch-all-Regel 'http_status:404' in $CONFIG gefunden. Bitte Regeln von Hand ergänzen (docs/deployment.md)."
else
  warn "$CONFIG fehlt. Container werden eingerichtet, Tunnel-Regeln bitte von Hand ergänzen."
fi

# Erreichbarkeit der bestehenden Hostnamen vor der Änderung festhalten
declare -A BEFORE
EXISTING=()
if [ -f "$CONFIG" ]; then
  mapfile -t EXISTING < <(awk '/hostname:/{print $NF}' "$CONFIG" | grep -vE "^($SITE_HOST|$API_HOST)$" || true)
  for h in "${EXISTING[@]}"; do
    BEFORE[$h]=$(curl -s -o /dev/null -m 15 -w '%{http_code}' "https://$h/" || echo 000)
    echo "   vorher $h -> ${BEFORE[$h]}"
  done
fi

# ---------------------------------------------------------------- 2. Container
step "2/4 Anwendung installieren"
if [ ! -d "$DIR" ]; then
  sudo mkdir -p "$DIR"
  sudo chown "$(id -un)": "$DIR"
  ok "$DIR angelegt"
fi
cp "$UPLOAD/docker-compose.yml" "$UPLOAD/Dockerfile.prebuilt" "$UPLOAD/.env.example" "$DIR/"
rm -rf "$DIR/app"
mkdir -p "$DIR/app"
cp -r "$UPLOAD/app/api" "$UPLOAD/app/web" "$DIR/app/"
ok "Programmdateien kopiert"

if [ ! -f "$DIR/.env" ]; then
  KEY=$(openssl rand -base64 36 | tr -d '=+/\n' | cut -c1-40)
  {
    echo "# Angelegt von pi-setup.sh am $(date '+%d.%m.%Y %H:%M')"
    echo "ADMIN_API_KEY=$KEY"
    echo "SITE_PUBLIC_URL=https://$SITE_HOST"
    echo "API_PUBLIC_URL=https://$API_HOST"
    echo "WEB_PORT=$WEB_PORT"
    echo "API_PORT=$API_PORT"
    echo "SEED_CONTENT=true"
    echo "ALLOW_INDEXING=false"
    echo "BUILD_CONTEXT=."
    echo "DOCKERFILE=Dockerfile.prebuilt"
    if [ -f "$UPLOAD/legal.env" ]; then cat "$UPLOAD/legal.env"; else printf 'LEGAL_NAME=\nLEGAL_STREET=\nLEGAL_CITY=\nLEGAL_EMAIL=\nLEGAL_PHONE=\n'; fi
  } > "$DIR/.env"
  chmod 600 "$DIR/.env"
  ok ".env mit neuem Verwaltungsschlüssel angelegt (nur auf dem Pi gespeichert)"
else
  ok ".env vorhanden, bleibt unverändert"
fi
rm -f "$UPLOAD/legal.env"

cd "$DIR"
docker compose up -d --build --remove-orphans
for _ in $(seq 1 45); do
  w=$(curl -s -m 3 "http://127.0.0.1:$WEB_PORT/health" || true)
  a=$(curl -s -m 3 "http://127.0.0.1:$API_PORT/health" || true)
  [ "$w" = Healthy ] && [ "$a" = Healthy ] && break
  sleep 2
done
[ "$a" = Healthy ] || { docker compose logs --tail 40 story-api; fail "API antwortet nicht auf 127.0.0.1:$API_PORT"; }
[ "$w" = Healthy ] || { docker compose logs --tail 40 story-web; fail "Website antwortet nicht auf 127.0.0.1:$WEB_PORT"; }
ok "API und Website laufen lokal"
[ "$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$WEB_PORT/")" = 200 ] && ok "Startseite lokal: 200" || warn "Startseite lokal nicht 200"

# ---------------------------------------------------------------- 3. Tunnel
step "3/4 Cloudflare Tunnel"
if [ -z "$TUNNEL" ]; then
  warn "Übersprungen (keine Tunnel-Konfiguration)."
elif grep -qE "hostname:[[:space:]]*${SITE_HOST}[[:space:]]*$" "$CONFIG" && grep -qE "hostname:[[:space:]]*${API_HOST}[[:space:]]*$" "$CONFIG"; then
  ok "Regeln für $SITE_HOST und $API_HOST bestehen schon, keine Änderung"
else
  BACKUP="$CONFIG.bak.$STAMP"
  sudo cp "$CONFIG" "$BACKUP"
  ok "Sicherung: $BACKUP"
  NEW=$(mktemp)
  awk -v site="$SITE_HOST" -v api="$API_HOST" -v wp="$WEB_PORT" -v ap="$API_PORT" '
    !done && /^[[:space:]]*-[[:space:]]*service:[[:space:]]*http_status:404/ {
      match($0, /^[[:space:]]*/); ind = substr($0, 1, RLENGTH)
      print ind "- hostname: " site; print ind "  service: http://localhost:" wp
      print ind "- hostname: " api;  print ind "  service: http://localhost:" ap
      done = 1
    }
    { print }' "$CONFIG" > "$NEW"
  if ! cloudflared tunnel --config "$NEW" ingress validate; then
    rm -f "$NEW"; fail "Neue Tunnel-Konfiguration ungültig. Nichts geändert."
  fi
  sudo cp "$NEW" "$CONFIG"; rm -f "$NEW"
  ok "Regeln eingefügt und geprüft"
  for h in "$SITE_HOST" "$API_HOST"; do
    if out=$(cloudflared tunnel route dns "$TUNNEL" "$h" 2>&1); then ok "DNS-Route $h"; else
      echo "$out" | grep -qi "already exists" && ok "DNS-Eintrag $h existiert bereits" || warn "DNS-Route $h: $out"
    fi
  done
  sudo systemctl restart cloudflared
  sleep 8
  systemctl is-active --quiet cloudflared || {
    sudo cp "$BACKUP" "$CONFIG"; sudo systemctl restart cloudflared
    fail "cloudflared startete nicht. Alte Konfiguration wiederhergestellt."
  }
  ok "cloudflared neu gestartet"
fi

# ---------------------------------------------------------------- 4. Prüfen
step "4/4 Alle Seiten prüfen"
BROKEN=()
for h in "${EXISTING[@]}"; do
  after=$(curl -s -o /dev/null -m 15 -w '%{http_code}' "https://$h/" || echo 000)
  before=${BEFORE[$h]}
  echo "   $h: vorher $before, jetzt $after"
  if [[ "$before" =~ ^[234] ]] && ! [[ "$after" =~ ^[234] ]]; then BROKEN+=("$h"); fi
done
if [ ${#BROKEN[@]} -gt 0 ] && [ -n "${BACKUP:-}" ]; then
  sudo cp "$BACKUP" "$CONFIG"; sudo systemctl restart cloudflared
  fail "Diese Seiten antworteten danach nicht mehr: ${BROKEN[*]}. Alte Tunnel-Konfiguration wiederhergestellt."
fi

PUBLIC_OK=true
for _ in $(seq 1 12); do
  s=$(curl -s -o /dev/null -m 10 -w '%{http_code}' "https://$SITE_HOST/" || echo 000)
  a=$(curl -s -o /dev/null -m 10 -w '%{http_code}' "https://$API_HOST/v1/channel" || echo 000)
  [ "$s" = 200 ] && [ "$a" = 200 ] && break
  sleep 5
done
[ "$s" = 200 ] && ok "https://$SITE_HOST -> 200" || { warn "https://$SITE_HOST -> $s (DNS kann ein paar Minuten brauchen)"; PUBLIC_OK=false; }
[ "$a" = 200 ] && ok "https://$API_HOST -> 200" || { warn "https://$API_HOST -> $a"; PUBLIC_OK=false; }

echo
echo "Fertig. Website: https://$SITE_HOST   API: https://$API_HOST"
echo "Verwaltungsschlüssel anzeigen: grep ADMIN_API_KEY $DIR/.env"
$PUBLIC_OK || echo "Hinweis: Öffentliche Adressen noch nicht erreichbar, siehe WARN oben."
