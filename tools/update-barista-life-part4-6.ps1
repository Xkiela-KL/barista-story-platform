<#
.SYNOPSIS
    Trägt Barista Life Part 4–6 auf der laufenden Website ein und kündigt Part 7 an.

.DESCRIPTION
    Lädt die Vorschaubilder aus src/StoryPlatform.Api/SeedMedia hoch (nur wenn sie in der Mediathek noch fehlen),
    holt die Serie barista-life von der API, ersetzt den Platzhalter "Part 4 · Titel folgt", fügt Part 4–6 hinzu,
    kündigt Part 7 an und speichert die Serie wieder. Bereits vorhandene Folgen (gleiches Kürzel) bleiben unverändert,
    echte Termine und Links gehen also nicht verloren. Vorher wird eine Sicherung des gesamten Inhalts geschrieben.

    Neue Termine sind Beispieltermine (isPlaceholder = true), wie bisher bei Part 1–3.

.EXAMPLE
    pwsh tools/update-barista-life-part4-6.ps1 -Key "DEIN-SCHLÜSSEL"
    pwsh tools/update-barista-life-part4-6.ps1 -Api http://localhost:5087 -Key dev-key-nur-lokal
#>
param(
    [string] $Api = "https://api.xkiela.dev",
    [Parameter(Mandatory)] [string] $Key,
    [switch] $WhatIf
)
$ErrorActionPreference = "Stop"
$headers = @{ "X-Api-Key" = $Key }
$media = Join-Path $PSScriptRoot "..\src\StoryPlatform.Api\SeedMedia"

# --- Sicherung -------------------------------------------------------------------------------------
$content = Invoke-RestMethod "$Api/v1/admin/content" -Headers $headers
$backup = "backup-content-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$content | ConvertTo-Json -Depth 30 | Set-Content $backup -Encoding utf8
Write-Host "Sicherung: $backup"

# --- Bilder ------------------------------------------------------------------------------------------
$existing = @((Invoke-RestMethod "$Api/v1/admin/media" -Headers $headers) | ForEach-Object fileName)
$thumbs = @{}
foreach ($n in 4, 5, 6) {
    $name = "barista-life-part-$n.jpg"
    if ($existing -contains $name) { $thumbs[$n] = $name; Write-Host "Bild vorhanden: $name"; continue }
    if ($WhatIf) { $thumbs[$n] = $name; Write-Host "Würde hochladen: $name"; continue }
    $r = Invoke-RestMethod "$Api/v1/admin/media" -Method Post -Headers $headers -Form @{ file = Get-Item (Join-Path $media $name) }
    $thumbs[$n] = $r.fileName
    Write-Host "Hochgeladen: $($r.fileName)"
}

# --- Termine (Beispielwerte, 18:00 Uhr Berliner Zeit) ------------------------------------------------
try { $tz = [TimeZoneInfo]::FindSystemTimeZoneById("Europe/Berlin") }
catch { $tz = [TimeZoneInfo]::FindSystemTimeZoneById("W. Europe Standard Time") }
$today = [TimeZoneInfo]::ConvertTime([DateTimeOffset]::UtcNow, $tz).Date
function At18([datetime] $day) {
    $local = [datetime]::SpecifyKind($day.AddHours(18), [DateTimeKind]::Unspecified)
    ([DateTimeOffset]::new($local, $tz.GetUtcOffset($local))).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
}
$nextWed = $today.AddDays(1)
while ($nextWed.DayOfWeek -ne [DayOfWeek]::Wednesday) { $nextWed = $nextWed.AddDays(1) }

$newEpisodes = @(
    [ordered]@{ slug = "part-4-das-erwachen"; number = 4; title = "Das Erwachen"; durationSeconds = 69
        synopsis = "Ein Schuss, ein weißer Blitz. Dann stehst du wieder hinter deinem Tresen. Ein Stammkunde will seinen schwarzen Kaffee wie immer, die Kaffeemaschine ist plötzlich uralt, und die Wanduhr zeigt 19:58. Es ist dein Café. Aber nicht deine Zeit."
        releaseAt = (At18 $today.AddDays(-6)); thumbnail = $thumbs[4]; isPlaceholder = $true }
    [ordered]@{ slug = "part-5-der-mann-in-der-ecke"; number = 5; title = "Der Mann in der Ecke"; durationSeconds = 65
        synopsis = "Es ist 1997, und du willst wissen, warum. Der Stammkunde warnt dich leise: Ein Mann in der Ecke lässt dich seit einer Stunde nicht aus den Augen. Dann kommt er an den Tresen und schiebt dir ein Polaroid hin. Ein Foto von dir, heute Abend, durch dein eigenes Fenster aufgenommen."
        releaseAt = (At18 $today.AddDays(-3)); thumbnail = $thumbs[5]; isPlaceholder = $true }
    [ordered]@{ slug = "part-6-nur-gestreift"; number = 6; title = "Nur gestreift"; durationSeconds = 64
        synopsis = "Blaulicht, ein Sanitäter, ein Verband am Arm. Der Schuss hat dich nur gestreift, du warst nur ein paar Minuten weg. Doch die fremde Frau ist verschwunden, und mit ihr alle Überwachungsaufnahmen der letzten Woche. Dann vibriert dein Handy."
        releaseAt = (At18 $today); thumbnail = $thumbs[6]; isPlaceholder = $true }
    [ordered]@{ slug = "part-7"; number = 7; title = "Titel folgt"; synopsis = "Die Fortsetzung von „Nur gestreift“."
        releaseAt = (At18 $nextWed); label = "Fortsetzung"; isPlaceholder = $true }
)

# --- Serie anpassen ----------------------------------------------------------------------------------
$series = $content.series | Where-Object slug -eq "barista-life"
if (-not $series) { throw "Serie barista-life nicht gefunden." }
$season = $series.seasons | Where-Object number -eq 1

# Den alten Platzhalter "part-4 · Titel folgt" entfernen, damit Nummer 4 frei wird.
$episodes = [System.Collections.ArrayList]@($season.episodes | Where-Object { -not ($_.slug -eq "part-4" -and $_.title -eq "Titel folgt") })
foreach ($e in $newEpisodes) {
    if ($episodes | Where-Object { $_.slug -eq $e.slug }) { Write-Host "Bleibt unverändert: $($e.slug)"; continue }
    if ($episodes | Where-Object { $_.number -eq $e.number }) { Write-Warning "Nummer $($e.number) ist schon belegt, $($e.slug) wird übersprungen."; continue }
    [void]$episodes.Add([pscustomobject]$e)
    Write-Host "Neu: Part $($e.number) · $($e.title)"
}
$season.episodes = @($episodes | Sort-Object number)

$series.description = $series.description -replace "Eine Krimiserie in Fortsetzungen, erzählt aus deiner Sicht\.$",
    "Eine illustrierte Krimiserie in Fortsetzungen, erzählt aus deiner Sicht. Die Szenen sind gezeichnet und zeigen nicht das Spiel."

$body = $series | ConvertTo-Json -Depth 30
if ($WhatIf) { $body | Set-Content "barista-life-neu.json" -Encoding utf8; Write-Host "Nur Probe: barista-life-neu.json geschrieben, nichts gespeichert."; return }
Invoke-RestMethod "$Api/v1/admin/series/barista-life" -Method Put -Headers $headers `
    -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes($body)) | Out-Null
Write-Host "Gespeichert. Die Website zeigt die Änderung nach wenigen Sekunden."
