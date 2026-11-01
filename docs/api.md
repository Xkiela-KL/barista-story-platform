# REST-API

Basis-URL: `https://api.xkiela.dev` (lokal `http://localhost:5087`). Antworten sind JSON, Zeiten UTC (ISO 8601).

## Öffentlich (nur lesen, 60 s gecacht)

| Methode | Pfad | Inhalt |
| --- | --- | --- |
| GET | `/v1/channel` | Name, Claim, Social-Links, `hasPlaceholderContent` |
| GET | `/v1/featured` | Serie für den Startseiten-Hero mit nächster Folge (204, wenn es keine Serie gibt) |
| GET | `/v1/series` | Alle Serien als Übersicht |
| GET | `/v1/series/{slug}` | Serie mit Staffeln und Folgen |
| GET | `/v1/series/{slug}/episodes/{episode}` | Eine Folge |
| GET | `/v1/episodes/latest?limit=12` | Zuletzt veröffentlichte Folgen |
| GET | `/v1/schedule?from=2026-11-01&days=7` | Programmplan ab einem Berliner Kalendertag (1–60 Tage) |
| GET | `/media/{datei}` | Bilder der Mediathek (1 Tag Cache) |
| GET | `/health` | Betriebszustand |

`status` einer Folge: `Published` (Termin vorbei), `Scheduled` (Termin in der Zukunft), `Unscheduled` (kein Termin).
`videoSources` werden erst ab Veröffentlichung ausgeliefert.

## Verwaltung (Header `X-Api-Key`)

| Methode | Pfad | Inhalt |
| --- | --- | --- |
| GET | `/v1/admin/content` | Gesamter Inhalt (Export, Backup) |
| PUT | `/v1/admin/content` | Gesamten Inhalt ersetzen (Import) |
| PUT | `/v1/admin/channel` | Sender: Name, Claim, Beschreibung, Social-Links |
| PUT | `/v1/admin/series/{slug}` | Serie inklusive Staffeln und Folgen anlegen oder ersetzen |
| DELETE | `/v1/admin/series/{slug}` | Serie löschen |
| GET | `/v1/admin/media` | Dateien der Mediathek |
| POST | `/v1/admin/media` | Bild hochladen (multipart, Feld `file`, jpg/png/webp, max. 8 MB) |

Fehler: `401` falscher Schlüssel, `422` ungültige Inhalte mit deutscher Fehlerliste, `429` zu viele Anfragen,
`503` Schlüssel auf dem Server nicht gesetzt. Jede Änderung leert den Cache; die Website zeigt sie nach spätestens
30 Sekunden.

## Datenmodell (Auszug)

```json
{
  "slug": "barista-life",
  "title": "Barista Life",
  "tagline": "„Bitte tu so, als würdest du mich kennen.“",
  "genres": ["Krimi", "Mystery"],
  "state": "Running",
  "coverImage": "barista-life-cover.jpg",
  "heroImage": "barista-life-hero.jpg",
  "socialImage": "barista-life-social.jpg",
  "episodeTerm": "Part",
  "featuredRank": 1,
  "seasons": [{
    "number": 1,
    "episodes": [{
      "slug": "part-4",
      "number": 4,
      "title": "Titel folgt",
      "synopsis": "…",
      "durationSeconds": null,
      "releaseAt": "2026-11-04T17:00:00Z",
      "label": "Fortsetzung",
      "thumbnail": null,
      "videoSources": [{ "platform": "TikTok", "url": "https://www.tiktok.com/@…/video/…" }],
      "access": "Free",
      "isPlaceholder": false
    }]
  }]
}
```

Bildfelder enthalten einen Dateinamen aus der Mediathek oder eine `https://`-Adresse.
`releaseAt` 18:00 Uhr Berliner Zeit ist im Winter `17:00Z`, im Sommer `16:00Z`.
