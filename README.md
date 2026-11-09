# XKStories – Story-Plattform

Website und REST-API für **XKStories**, die Marke für illustrierte Krimi- und Mystery-Serien.
Erste Serie: **Barista Life** (Part 1 „19 Uhr 58“, Part 2 „Der Anruf“, Part 3 „Die Falle“, Part 4 „Das Erwachen“,
Part 5 „Der Mann in der Ecke“, Part 6 „Nur gestreift“).

- Website: `stories.xkiela.dev` (Blazor, statisches Server-Rendering)
- API: `api.xkiela.dev` (ASP.NET Core Minimal API)
- Hosting: Docker auf dem Raspberry Pi hinter dem bestehenden Cloudflare Tunnel

## Aufbau

| Projekt | Inhalt |
| --- | --- |
| `src/StoryPlatform.Contracts` | Gemeinsame Datenmodelle (Dokumente zum Pflegen, Views zum Lesen), Zeitzone Europe/Berlin |
| `src/StoryPlatform.Api` | REST-API: Serien, Staffeln, Folgen, Programmplan, Mediathek, Verwaltung per API-Schlüssel |
| `src/StoryPlatform.Web` | Website: Startseite, Programm, Serien, Folgen, Impressum, Datenschutz, Sitemap |
| `deploy/` | Dockerfiles, `docker-compose.yml`, `deploy.sh`, `.env.example` |
| `tools/prepare_series_media.py` | Erzeugt die Web-Bilder aus den Szenenbildern der Folgenordner |
| `docs/` | [Implementierungsplan](docs/implementierungsplan.md), [API](docs/api.md), [Inhalte pflegen](docs/inhalte-pflegen.md), [Deployment](docs/deployment.md) |

## Lokal starten

Voraussetzung: .NET 10 SDK.

```powershell
dotnet run --project src/StoryPlatform.Api   # http://localhost:5087
dotnet run --project src/StoryPlatform.Web   # http://localhost:5086
```

Beim ersten Start legt die API `src/StoryPlatform.Api/data/content.json` an: Barista Life mit Part 1–6
und einem angekündigten Part 7. **Die Sendetermine sind Beispielwerte** und auf der Website als
„Beispieltermin“ markiert, bis echte Termine eingetragen sind (siehe [Inhalte pflegen](docs/inhalte-pflegen.md)).
Lokaler Verwaltungsschlüssel: `dev-key-nur-lokal`.

## Grundsätze

- Inhalte stehen nie im HTML, sondern kommen aus der API (`content.json` + Mediathek im Docker-Volume).
- Der Programmplan wird aus den Veröffentlichungszeiten der Folgen berechnet. Es gibt keine zweite Terminliste.
- Alle Zeiten werden in UTC gespeichert und in Europe/Berlin angezeigt.
- Keine Cookies, kein Tracking, keine Schriften oder Skripte von fremden Servern.
- Gestaltung nach den Folgenordnern von Barista Life: Endtafel-Titel in Georgia, Untertitel-Boxen,
  bernsteinfarbene Sprecher-Labels, REC-Punkt aus dem Kamera-Overlay.
