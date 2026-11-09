# Inhalte pflegen

Alles läuft über die API. Die Beispiele sind für PowerShell 7 (`pwsh`) auf deinem PC; `$key` ist der `ADMIN_API_KEY` aus
`deploy/.env` (lokal: `dev-key-nur-lokal`, Basis `http://localhost:5087`).

```powershell
$api = "https://api.xkiela.dev"
$key = @{ "X-Api-Key" = "DEIN-SCHLÜSSEL" }
```

## Serie laden, bearbeiten, speichern

Der einfachste Weg: die Serie als Datei holen, in VS Code bearbeiten, zurückschicken.

```powershell
# 1. Gesamten Inhalt sichern und die Serie herausziehen
Invoke-RestMethod "$api/v1/admin/content" -Headers $key | ConvertTo-Json -Depth 20 | Set-Content backup.json -Encoding utf8
(Get-Content backup.json -Raw | ConvertFrom-Json).series | Where-Object slug -eq "barista-life" |
    ConvertTo-Json -Depth 20 | Set-Content barista-life.json -Encoding utf8

# 2. barista-life.json bearbeiten (Termine, Links, Texte)

# 3. Speichern – ungültige Eingaben werden mit einer Fehlerliste abgelehnt
Invoke-RestMethod "$api/v1/admin/series/barista-life" -Method Put -Headers $key `
    -ContentType "application/json; charset=utf-8" -Body (Get-Content barista-life.json -Raw -Encoding utf8)
```

## Echten Sendetermin eintragen

In der Folge `releaseAt` setzen und `isPlaceholder` auf `false` stellen:

```json
"releaseAt": "2026-11-04T17:00:00Z",
"isPlaceholder": false
```

`releaseAt` ist UTC: Berliner Zeit minus 1 Stunde im Winter (MEZ), minus 2 Stunden im Sommer (MESZ).
Ohne Termin `"releaseAt": null` – dann steht „Termin folgt“ auf der Seite.

## Link zur Folge eintragen

```json
"videoSources": [
  { "platform": "TikTok", "url": "https://www.tiktok.com/@konto/video/123" },
  { "platform": "Instagram", "url": "https://www.instagram.com/reel/abc/" }
]
```

Die Links erscheinen automatisch ab dem Veröffentlichungstermin.

## Instagram- und TikTok-Profil des Senders

```powershell
$channel = @{
  name = "XKStories"; claim = "Krimi und Mystery in Serie"
  description = "Illustrierte Krimi- und Mystery-Geschichten in Fortsetzungen, mit festen Sendeterminen."
  socialLinks = @(
    @{ platform = "Instagram"; url = "https://www.instagram.com/KONTO/"; handle = "@KONTO" },
    @{ platform = "TikTok"; url = "https://www.tiktok.com/@KONTO"; handle = "@KONTO" }
  )
} | ConvertTo-Json -Depth 5
Invoke-RestMethod "$api/v1/admin/channel" -Method Put -Headers $key -ContentType "application/json; charset=utf-8" -Body $channel
```

## Bilder hochladen

Cover 2:3 (z. B. 800×1200), Szenenbilder 2:3, Social-Vorschau 1200×630, JPG/PNG/WebP bis 8 MB.

```powershell
Invoke-RestMethod "$api/v1/admin/media" -Method Post -Headers $key -Form @{ file = Get-Item .\part-4.jpg }
# Antwort: fileName "part-4.jpg" -> in der Folge als "thumbnail": "part-4.jpg" eintragen
```

Für Barista Life erzeugt `python tools/prepare_series_media.py "X:\Packages\BaristaLife-Storyclips\Folgen"`
die Web-Bilder aus den Szenenbildern der Folgenordner.

## Neue Serie

`PUT /v1/admin/series/{slug}` mit einem neuen Kürzel (nur `a–z`, `0–9`, `-`). `featuredRank: 1` setzt sie in den
Hero der Startseite; die niedrigste Zahl gewinnt. `episodeTerm` ist `"Folge"` oder z. B. `"Part"`.

## Beispieltermine entfernen

Solange eine Folge `isPlaceholder: true` hat, steht „Beispieltermin“ daneben und oben auf der Seite ein Hinweis.
Sind alle auf `false`, verschwindet der Hinweis von selbst.

## Barista Life Part 4–6 nachtragen

Auf einer bestehenden Installation (mit vorhandener `content.json`) trägt dieses Skript Part 4–6 ein, ersetzt den
Platzhalter „Part 4 · Titel folgt“ und kündigt Part 7 an. Es lädt die Vorschaubilder hoch, legt vorher eine Sicherung
an und lässt vorhandene Folgen mit gleichem Kürzel unverändert.

```powershell
pwsh tools/update-barista-life-part4-6.ps1 -Key "DEIN-SCHLÜSSEL" -WhatIf   # Probe: schreibt nur barista-life-neu.json
pwsh tools/update-barista-life-part4-6.ps1 -Key "DEIN-SCHLÜSSEL"
```
