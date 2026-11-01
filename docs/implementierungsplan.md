# Implementierungsplan XKStories

Stand: 1. November 2026

## 1. Ausgangslage (geprüft)

| Bereich | Befund |
| --- | --- |
| REST-API | Es gab noch keine API für die Plattform. Sie wird in diesem Repo neu gebaut. Das GitHub-Repo `API-123` ist leer und wird nicht verwendet. |
| Hosting | Raspberry Pi (`xkiela@raspberrypi`, Debian 13, arm64, 1,8 GB RAM). Apps laufen als systemd-Dienste unter `/opt` oder in Docker auf `127.0.0.1`. Docker ist seit dem `training-planner` installiert. Quelle: `training-planner/docs/deployment.md`, Stand 5.10.2026. |
| Öffentliches Routing | Cloudflare Tunnel (`cloudflared.service`, lokale Konfiguration `/etc/cloudflared/config.yml`) mit Catch-all-404. Keine offenen Ports im Heimnetz. |
| DNS | `xkiela.dev` bei Cloudflare (Nameserver `athena`/`tate`). Jede Subdomain löst auf Cloudflare-Adressen auf, es gibt also einen Wildcard-Eintrag. `tv.`, `stories.` und `api.xkiela.dev` haben noch keine Tunnel-Regel (Abruf schlägt fehl). |
| Cloudflare-Konto | Keine Workers, keine D1-Datenbanken, R2 nicht aktiviert. DNS-Einträge und Tunnel-Konfiguration waren von hier aus nicht lesbar. **Vor dem Deployment auf dem Pi prüfen** (siehe Deployment). |
| Stack-Vorbild | `training-planner`: .NET 10, Blazor, EF Core, PostgreSQL, Docker Compose, Deployment per `deploy.sh`. |
| Inhalte | `X:\Packages\BaristaLife-Storyclips\Folgen` mit Part 1–3 (Bilder 1024×1536, Renders 1080×1920, Untertitel-Stile). |

## 2. Entscheidungen

**Subdomain: `tv.xkiela.dev` für die Website, `api.xkiela.dev` für die API.**
`tv.` passt zur Vision eines eigenen Senders, ist kurz und am Telefon leicht zu sagen. Verschachtelte Namen wie
`api.stories.xkiela.dev` scheiden aus: Das kostenlose Universal-SSL-Zertifikat von Cloudflare deckt nur eine
Subdomain-Ebene ab. Langfristig ist eine eigene Marken-Domain (z. B. `xkstories.de`) sinnvoll; die Adressen sind
nur Konfiguration (`SITE_PUBLIC_URL`, `API_PUBLIC_URL`), ein Umzug braucht dann eine 301-Weiterleitung in Cloudflare.

**Blazor mit statischem Server-Rendering statt Blazor Server oder WebAssembly.**
Jede Seite ist fertiges HTML: schnell auf dem Handy, gut für Suchmaschinen und Vorschaubilder, keine
WebSocket-Verbindung pro Besucher (wichtig bei 1,8 GB RAM). Interaktive Komponenten lassen sich später für
Mitgliederbereiche einzeln zuschalten.

**Klare Trennung von Website und API.**
Die Website holt alle Inhalte über einen typisierten Client aus der API, intern im Docker-Netz
(`http://story-api:8080`). Die API ist zusätzlich öffentlich erreichbar für Bilder, spätere Apps und die Verwaltung.
Gemeinsame Modelle liegen in `StoryPlatform.Contracts`, es gibt keine doppelten Strukturen.

**Datenhaltung V1: eine JSON-Datei im Volume, hinter `IContentStore`.**
Für eine Handvoll Serien reicht das, es braucht keinen zusätzlichen Datenbank-Container und ist leicht zu sichern.
Änderungen werden als Ganzes validiert und atomar geschrieben. Sobald Konten und Abos kommen, ersetzt eine
EF-Core/PostgreSQL-Implementierung diese eine Klasse; Endpunkte und Website bleiben gleich.

**Programmplan wird berechnet.**
Der Sendeplan ergibt sich aus `releaseAt` der Folgen. Status (Veröffentlicht, Demnächst, Termin folgt) berechnet die
API aus der aktuellen Zeit. Gibt es keine Termine, zeigt die Website einen Hinweis statt erfundener Daten.
Beispieltermine tragen `isPlaceholder: true` und werden als „Beispieltermin“ markiert.

**Für später vorbereitet, aber nicht umgesetzt:**
`videoSources` je Folge (`Instagram`, `TikTok`, `YouTube`, später `Hosted` für eigenes Video-Hosting),
`access` je Folge (`Free`, später `Members` für Abos und früheren Zugriff), Serienstatus, Social-Links pro Serie.

## 3. Umsetzung V1 (erledigt)

1. Lösung mit drei Projekten, gemeinsame Modelle, Zeitzonen-Hilfen (`BerlinTime`, Umstellung Sommer-/Winterzeit geprüft).
2. REST-API: öffentliche Lese-Endpunkte mit 60 s Output-Cache und `Cache-Control`, Verwaltung mit API-Schlüssel
   (konstante Vergleichszeit, Rate-Limit 60/min), Mediathek mit Upload-Prüfung (Endung + Dateisignatur, 8 MB),
   CORS-Whitelist, Health-Check, Startinhalt Barista Life.
3. Website: Startseite (Hero mit nächster Folge und Countdown, Heute im Programm, Demnächst, Neue Folgen, Serien,
   Social-Links), Programm (14 Tage), Serienübersicht, Serienseite mit Staffeln und Folgen, Folgenseite,
   Impressum- und Datenschutz-Vorlage, 404, Wartungshinweis mit HTTP 503 bei API-Ausfall.
4. SEO und Teilen: Titel, Beschreibung, Canonical, Open Graph, schema.org (`TVSeries`, `TVEpisode`),
   `sitemap.xml`, `robots.txt` (Indexierung erst nach Freigabe).
5. Sicherheit: Content-Security-Policy, keine Cookies, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`.
6. Deployment: Docker Compose für den Pi, `deploy.sh`, Anleitung für Cloudflare Tunnel.

## 4. Nächste Schritte

| Schritt | Wer |
| --- | --- |
| `deploy/.env` auf dem Pi anlegen (API-Schlüssel, Impressumsangaben) | Du |
| Ports 8086/8087 auf dem Pi prüfen, Tunnel-Regeln und DNS-Routen anlegen | Du (Befehle in `docs/deployment.md`) |
| Echte Sendetermine und Instagram-/TikTok-Links eintragen | Du (Anleitung in `docs/inhalte-pflegen.md`) |
| Impressum und Datenschutz rechtlich prüfen lassen, dann `ALLOW_INDEXING=true` | Du |
| Optional: Cloudflare Access vor `api.xkiela.dev/v1/admin*` | Du |

## 5. Ausbaustufen später

1. **Verwaltungsoberfläche** (Blazor, interaktiv, nur für dich): Folgen, Termine und Bilder per Formular statt per API-Aufruf.
2. **PostgreSQL** statt JSON-Datei, Migration der `content.json` per Import.
3. **Konten** (ASP.NET Core Identity wie im `training-planner`), danach **Abos** (Zahlungsanbieter mit Webhooks),
   `access: Members`, früherer Zugriff über ein zweites Veröffentlichungsdatum je Folge.
4. **Eigene Videowiedergabe** (`Hosted`-Quelle, Dateien z. B. in Cloudflare R2, Player auf der Folgenseite).
