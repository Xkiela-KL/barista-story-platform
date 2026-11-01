# Deployment auf dem Raspberry Pi

Gleiches Muster wie beim `training-planner`: auf dem PC veröffentlichen, auf dem Pi nur verpacken und starten,
öffentlich erreichbar über den bestehenden Cloudflare Tunnel. Es werden keine Ports im Heimnetz geöffnet.

```
Besucher ─https─> Cloudflare ─Tunnel─> cloudflared (Pi) ─> 127.0.0.1:8086  story-web ─intern─> story-api
                                                        └> 127.0.0.1:8087  story-api (Bilder, Verwaltung)
```

## Schnellweg: Installer (empfohlen)

`deploy/XKStories-installieren.cmd` doppelklicken (liegt auch unter `X:\temp\XKStories`). Er erledigt die Schritte 1–4
unten in einem Durchgang und bricht ab, bevor er etwas ändert, wenn etwas nicht passt:

1. prüft Git, .NET 10 SDK, ssh/scp und die Verbindung zum Pi
2. holt den Code nach `X:\Repository\barista-story-platform` und baut API und Website
3. fragt beim ersten Mal die Impressumsangaben ab (Enter = später)
4. führt auf dem Pi `deploy/pi-setup.sh` aus:
   - prüft Docker, die Ports 8086/8087 und ob `stories.`/`api.xkiela.dev` im Tunnel schon anders belegt sind
   - legt `/opt/xkstories/.env` mit einem neu erzeugten Verwaltungsschlüssel an (nur beim ersten Mal)
   - startet die Container und wartet auf `/health`
   - sichert `/etc/cloudflared/config.yml`, fügt die zwei Regeln vor der Catch-all-Regel ein, prüft sie mit
     `cloudflared tunnel ingress validate`, legt die DNS-Routen an und startet `cloudflared` neu
   - ruft alle bisherigen Hostnamen vorher und nachher auf; antwortet einer danach nicht mehr, wird die alte
     Konfiguration automatisch zurückgespielt
5. legt die Protokolle unter `X:\temp\xkstories-installation.log` (Pi) und `…-pc.log` ab

Ein erneuter Start aktualisiert die Anwendung; `.env` und die Tunnel-Regeln bleiben dann unverändert.
Den Verwaltungsschlüssel zeigt `ssh xkiela@raspberrypi "grep ADMIN_API_KEY /opt/xkstories/.env"`.
Wurde zurückgerollt, bleiben die DNS-Einträge `tv`/`api` bestehen; sie zeigen auf den Tunnel und schaden nicht.

Die folgenden Abschnitte beschreiben dieselben Schritte zum Nachvollziehen oder für die Einrichtung von Hand.

## 1. Bestand prüfen (nur lesen)

Auf dem PC in PowerShell. Schreibt die Übersicht nach `X:\temp\pi-inventar.txt`:

```powershell
ssh xkiela@raspberrypi "ss -tlnp; echo ---; ls -la /opt; echo ---; cat /etc/cloudflared/config.yml; echo ---; docker ps --format '{{.Names}} {{.Image}} {{.Ports}}'" | Out-File -Encoding utf8 X:\temp\pi-inventar.txt
```

Prüfen: Sind 8086 und 8087 frei? Gibt es schon Regeln für `stories.` oder `api.xkiela.dev`? Falls ein Port belegt
ist, `WEB_PORT`/`API_PORT` in `.env` ändern und in der Tunnel-Regel unten übernehmen.

Im Cloudflare-Dashboard unter DNS nachsehen, worauf der Wildcard-Eintrag `*.xkiela.dev` zeigt. Zeigt er bereits
auf den Tunnel (`<tunnel-id>.cfargotunnel.com`), reichen die Ingress-Regeln; die DNS-Routen in Schritt 3 legen
trotzdem eigene Einträge an und schaden nicht.

## 2. Einmalig einrichten

```bash
sudo mkdir -p /opt/xkstories && sudo chown xkiela: /opt/xkstories
# deploy/.env.example als /opt/xkstories/.env anlegen, dann:
chmod 600 /opt/xkstories/.env
# ADMIN_API_KEY setzen (openssl rand -base64 32), Impressum-Angaben eintragen,
# BUILD_CONTEXT=. und DOCKERFILE=Dockerfile.prebuilt setzen
```

## 3. Hostnamen im Tunnel

`/etc/cloudflared/config.yml` sichern und **vor** der Catch-all-Regel `http_status:404` ergänzen:

```yaml
  - hostname: stories.xkiela.dev
    service: http://localhost:8086
  - hostname: api.xkiela.dev
    service: http://localhost:8087
```

```bash
sudo cp /etc/cloudflared/config.yml /etc/cloudflared/config.yml.bak.$(date +%Y%m%d%H%M%S)
sudo nano /etc/cloudflared/config.yml
cloudflared tunnel ingress validate
cloudflared tunnel route dns <TUNNEL-ID> stories.xkiela.dev
cloudflared tunnel route dns <TUNNEL-ID> api.xkiela.dev
sudo systemctl restart cloudflared
```

Die Tunnel-ID steht in `config.yml` (`tunnel:`). Danach prüfen, dass alle bisherigen Hostnamen weiter antworten.

## 4. Veröffentlichen

Auf dem PC in Git Bash:

```bash
deploy/deploy.sh xkiela@raspberrypi /opt/xkstories
```

Prüfen auf dem Pi: `curl -fsS http://127.0.0.1:8086/health` und `curl -fsS http://127.0.0.1:8087/health` → `Healthy`.

## 5. Cloudflare-Einstellungen

| Einstellung | Wert | Grund |
| --- | --- | --- |
| SSL/TLS → Edge Certificates → Always Use HTTPS | an | Die Apps sprechen hinter dem Tunnel nur HTTP. |
| HSTS | optional an (max-age 6 Monate) | erst wenn alles läuft |
| Caching | Standard | Bilder (`/media/*.jpg`) werden mit `max-age=86400` am Rand gecacht. API-JSON und HTML tragen `max-age=60`; ohne Cache Rule cacht Cloudflare sie nicht, das ist für V1 richtig. |
| Zero Trust → Access (optional) | Anwendung für `api.xkiela.dev/v1/admin*` | Zweite Schutzschicht vor dem API-Schlüssel |
| Security → WAF Rate Limiting (optional) | `/v1/admin` | Die API begrenzt zusätzlich selbst auf 60 Anfragen/Minute. |

CORS: Die Website ruft die API serverseitig auf. Browser dürfen die öffentlichen GET-Endpunkte nur von
`SITE_PUBLIC_URL` aus abfragen; Bilder sind für alle Seiten freigegeben (Vorschaubilder in Messengern).

## Sicherung

```bash
docker run --rm -v xkstories_story-data:/data -v "$PWD":/backup alpine tar czf /backup/xkstories-$(date +%F).tgz -C /data .
```

oder über die API: `GET /v1/admin/content` (nur Texte und Termine, ohne Bilder).

## Zurückrollen

Tunnel-Backup zurückkopieren, `sudo systemctl restart cloudflared`, DNS-Einträge `tv` und `api` im Dashboard
löschen. App entfernen: `cd /opt/xkstories && docker compose down` (mit `-v` auch die Daten).
