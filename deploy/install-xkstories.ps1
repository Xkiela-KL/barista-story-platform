#Requires -Version 5.1
<#
  XKStories installieren oder aktualisieren – vom Windows-PC aus auf den Raspberry Pi.

  1. Holt den Code von GitHub nach X:\Repository\barista-story-platform (oder aktualisiert ihn)
  2. Baut API und Website (.NET 10 SDK)
  3. Fragt beim ersten Mal die Impressumsangaben ab (Enter = später eintragen)
  4. Lädt alles auf den Pi und führt dort deploy/pi-setup.sh aus:
     Prüfung -> Docker-Container -> Cloudflare-Tunnel-Regeln -> Kontrolle aller Seiten (mit Rückfall)
  5. Legt das Protokoll unter X:\temp\xkstories-installation.log ab

  Start: XKStories-installieren.cmd doppelklicken.
#>
param(
    [string]$PiHost = "xkiela@raspberrypi",
    [string]$Repo = "X:\Repository\barista-story-platform",
    [string]$RepoUrl = "https://github.com/Xkiela-KL/barista-story-platform.git",
    [string]$LogDir = "X:\temp"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$PcLog = Join-Path $LogDir "xkstories-installation-pc.log"
$PiLog = Join-Path $LogDir "xkstories-installation.log"
Start-Transcript -Path $PcLog -Force | Out-Null

function Step([string]$Text) { Write-Host ""; Write-Host "== $Text" -ForegroundColor Yellow }
function Ok([string]$Text) { Write-Host "   OK   $Text" -ForegroundColor Green }
function Need([string]$Cmd, [string]$Hint) {
    if (-not (Get-Command $Cmd -ErrorAction SilentlyContinue)) { throw "$Cmd wurde nicht gefunden. $Hint" }
}
function Invoke-Native([string]$Exe, [string[]]$Arguments) {
    # Native tools (git, scp) write progress to stderr; that must not count as a PowerShell error.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try { & $Exe @Arguments } finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "$Exe $($Arguments -join ' ') ist fehlgeschlagen (Exitcode $LASTEXITCODE)." }
}
function Write-Lf([string]$Source, [string]$Target) {
    # Shell-Skripte und Compose-Dateien brauchen Linux-Zeilenenden, auch wenn Git sie mit CRLF ausgecheckt hat.
    $text = [IO.File]::ReadAllText($Source) -replace "`r`n", "`n"
    [IO.File]::WriteAllText($Target, $text, (New-Object Text.UTF8Encoding $false))
}

$exitCode = 1
try {
    Write-Host "XKStories – Installation auf $PiHost" -ForegroundColor Cyan

    Step "1/5 Werkzeuge prüfen"
    Need git "Git für Windows installieren: https://git-scm.com/download/win"
    Need dotnet ".NET 10 SDK installieren: https://dotnet.microsoft.com/download/dotnet/10.0"
    Need ssh "OpenSSH-Client unter Einstellungen > Optionale Features aktivieren."
    Need scp "OpenSSH-Client unter Einstellungen > Optionale Features aktivieren."
    if (-not ((& dotnet --list-sdks) -match "^10\.")) { throw ".NET 10 SDK fehlt (dotnet --list-sdks)." }
    Ok "git, dotnet 10, ssh, scp"
    Invoke-Native ssh @("-o", "ConnectTimeout=10", $PiHost, "true")
    Ok "Verbindung zu $PiHost"

    Step "2/5 Code holen"
    if (Test-Path (Join-Path $Repo ".git")) {
        Invoke-Native git @("-C", $Repo, "pull", "--ff-only")
    } else {
        Invoke-Native git @("clone", $RepoUrl, $Repo)
    }
    Ok "$Repo ($(& git -C $Repo log -1 --format='%h %s'))"

    Step "3/5 API und Website bauen"
    $publish = Join-Path $Repo "artifacts\publish"
    if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
    foreach ($app in @(@{ Name = "api"; Project = "StoryPlatform.Api" }, @{ Name = "web"; Project = "StoryPlatform.Web" })) {
        $proj = Join-Path $Repo "src\$($app.Project)\$($app.Project).csproj"
        Invoke-Native dotnet @("publish", $proj, "-c", "Release", "-p:UseAppHost=false", "-o", (Join-Path $publish $app.Name), "--nologo", "-v", "q")
        Ok $app.Project
    }

    Step "4/5 Hochladen"
    $staging = Join-Path $env:TEMP "xkstories-upload"
    if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
    New-Item -ItemType Directory -Force -Path $staging | Out-Null
    foreach ($f in @("pi-setup.sh", "docker-compose.yml", "Dockerfile.prebuilt", ".env.example")) {
        Write-Lf (Join-Path $Repo "deploy\$f") (Join-Path $staging $f)
    }

    $ErrorActionPreference = "Continue"
    & ssh $PiHost "test -f /opt/xkstories/.env"
    $firstInstall = $LASTEXITCODE -ne 0
    $ErrorActionPreference = "Stop"
    if ($firstInstall) {
        Write-Host ""
        Write-Host "   Erste Installation: Angaben für Impressum und Datenschutz." -ForegroundColor Cyan
        Write-Host "   Enter lässt ein Feld leer; es steht dann als Lücke markiert auf der Seite und kann später" -ForegroundColor Cyan
        Write-Host "   in /opt/xkstories/.env auf dem Pi ergänzt werden." -ForegroundColor Cyan
        $legal = [ordered]@{
            LEGAL_NAME   = Read-Host "   Vor- und Nachname"
            LEGAL_STREET = Read-Host "   Straße und Hausnummer"
            LEGAL_CITY   = Read-Host "   PLZ und Ort"
            LEGAL_EMAIL  = Read-Host "   E-Mail-Adresse"
            LEGAL_PHONE  = Read-Host "   Telefon (optional)"
        }
        $lines = $legal.GetEnumerator() | ForEach-Object { "$($_.Key)=$(($_.Value -replace '[\r\n#]', ' ').Trim())" }
        [IO.File]::WriteAllText((Join-Path $staging "legal.env"), (($lines -join "`n") + "`n"), (New-Object Text.UTF8Encoding $false))
    }

    Invoke-Native ssh @($PiHost, "rm -rf ~/xkstories-upload && mkdir -p ~/xkstories-upload/app")
    $files = Get-ChildItem -Force $staging | ForEach-Object { $_.FullName }
    Invoke-Native scp (@("-q") + $files + @("${PiHost}:xkstories-upload/"))
    Invoke-Native scp @("-q", "-r", (Join-Path $publish "api"), (Join-Path $publish "web"), "${PiHost}:xkstories-upload/app/")
    Remove-Item -Recurse -Force $staging
    Ok "Dateien auf dem Pi"

    Step "5/5 Auf dem Pi einrichten (sudo fragt evtl. nach deinem Pi-Passwort)"
    $ErrorActionPreference = "Continue"
    & ssh -t $PiHost "bash ~/xkstories-upload/pi-setup.sh"
    $exitCode = $LASTEXITCODE
    & scp -q "${PiHost}:xkstories-upload/install.log" $PiLog
    $ErrorActionPreference = "Stop"
    if ($exitCode -ne 0) { throw "Einrichtung auf dem Pi abgebrochen. Details: $PiLog" }

    Write-Host ""
    Write-Host "Fertig: https://tv.xkiela.dev" -ForegroundColor Green
    Write-Host "Protokolle: $PiLog und $PcLog"
    Start-Process "https://tv.xkiela.dev"
    $exitCode = 0
}
catch {
    Write-Host ""
    Write-Host "FEHLER: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Protokolle: $PcLog (und $PiLog, falls vorhanden)"
    $exitCode = 1
}
finally {
    Stop-Transcript | Out-Null
}
exit $exitCode
