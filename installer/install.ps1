param (
    [switch]$DryRun
)

$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

function Get-StardewValleyPath {
    # 1. Controlla config.json locale dell'installer
    $configFile = Join-Path $PSScriptRoot "config.json"
    if (Test-Path $configFile) {
        try {
            $config = Get-Content $configFile | ConvertFrom-Json
            if ($config.GamePath -and (Test-Path $config.GamePath)) {
                return $config.GamePath
            }
        } catch {}
    }

    # 2. Percorso Steam Standard
    $steamDefault = "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
    if (Test-Path $steamDefault) { return $steamDefault }

    # 3. Librerie alternative di Steam (libraryfolders.vdf)
    $vdfPath = "C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf"
    if (Test-Path $vdfPath) {
        try {
            $content = Get-Content $vdfPath -Raw
            $matches = [regex]::Matches($content, '"path"\s+"([^"]+)"')
            foreach ($match in $matches) {
                $path = $match.Groups[1].Value.Replace("\\", "\")
                $potential = Join-Path $path "steamapps\common\Stardew Valley"
                if (Test-Path $potential) { return $potential }
            }
        } catch {}
    }

    # 4. Percorso GOG Standard
    $gogDefault = "C:\GOG Games\Stardew Valley"
    if (Test-Path $gogDefault) { return $gogDefault }

    # 5. Registro GOG
    try {
        $regPath = Get-ItemProperty -Path "HKLM:\SOFTWARE\GOG.com\Games\1453215229" -Name "path" -ErrorAction SilentlyContinue
        if ($regPath -and $regPath.path -and (Test-Path $regPath.path)) {
            return $regPath.path
        }
    } catch {}

    # 6. GOG Galaxy Standard
    $gogGalaxyDefault = "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley"
    if (Test-Path $gogGalaxyDefault) { return $gogGalaxyDefault }

    # 7. Percorso Xbox App Standard
    $xboxDefault = "C:\Program Files\ModifiableWindowsApps\Stardew Valley"
    if (Test-Path $xboxDefault) { return $xboxDefault }

    return $null
}

function Save-GamePath ($path) {
    $configFile = Join-Path $PSScriptRoot "config.json"
    $configObj = [PSCustomObject]@{
        GamePath = $path
    }
    $configObj | ConvertTo-Json | Out-File $configFile
}

function Get-InstalledVersion ($gamePath) {
    $manifestPath = Join-Path $gamePath "Mods\StardewNavigator\manifest.json"
    if (Test-Path $manifestPath) {
        try {
            $manifest = Get-Content $manifestPath | ConvertFrom-Json
            return $manifest.Version
        } catch {}
    }
    return $null
}

function Check-StardewAccess ($gamePath) {
    $saPath1 = Join-Path $gamePath "Mods\StardewAccess"
    $saPath2 = Join-Path $gamePath "Mods\stardew-access"
    if ((Test-Path $saPath1) -or (Test-Path $saPath2)) {
        return $true
    }
    return $false
}

function Get-LatestRelease {
    $uri = "https://api.github.com/repos/Nemex81/StardewNavigator/releases/latest"
    try {
        $release = Invoke-RestMethod -Uri $uri -Headers @{"User-Agent"="StardewNavigatorInstaller"}
        return $release
    } catch {
        Write-Error "Impossibile contattare le API di GitHub: $_"
        return $null
    }
}

# --- LOGICA DI DRY-RUN ---
if ($DryRun) {
    Write-Host "=== STARDEWNAVIGATOR INSTALLER: DRY-RUN MODE ===" -ForegroundColor Yellow
    
    $path = Get-StardewValleyPath
    if ($path) {
        Write-Host "Percorso rilevato automaticamente: $path" -ForegroundColor Green
        if (Test-Path (Join-Path $path "StardewModdingAPI.exe")) {
            Write-Host "SMAPI rilevato: SI" -ForegroundColor Green
        } else {
            Write-Host "SMAPI rilevato: NO (Verificare l'installazione di SMAPI)" -ForegroundColor Red
        }

        $version = Get-InstalledVersion $path
        if ($version) {
            Write-Host "Versione StardewNavigator installata: $version" -ForegroundColor Green
        } else {
            Write-Host "Versione StardewNavigator installata: Mod non installata" -ForegroundColor Yellow
        }

        $saPresent = Check-StardewAccess $path
        if ($saPresent) {
            Write-Host "Stardew Access rilevato: SI (Modalita screen reader attiva)" -ForegroundColor Green
        } else {
            Write-Host "Stardew Access rilevato: NO (Modalita visuale HUD attiva)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "Percorso Stardew Valley non rilevato automaticamente." -ForegroundColor Red
    }
    Write-Host "=== FINE DRY-RUN ===" -ForegroundColor Yellow
    exit 0
}

# --- FLUSSO INTERATTIVO STANDARD ---

Clear-Host
Write-Host "========================================="
Write-Host "   StardewNavigator Mod Installer v1.0   "
Write-Host "========================================="

$gamePath = Get-StardewValleyPath

if (-not $gamePath) {
    Write-Host "Percorso di Stardew Valley non rilevato automaticamente."
    $gamePath = Read-Host "Inserisci il percorso completo della cartella di Stardew Valley"
}

# Rimuove virgolette dal percorso se presenti
$gamePath = $gamePath.Trim("`"")

if (-not (Test-Path $gamePath) -or -not ((Test-Path (Join-Path $gamePath "Stardew Valley.exe")) -or (Test-Path (Join-Path $gamePath "StardewModdingAPI.exe")))) {
    Write-Host "Errore: Il percorso inserito non sembra essere una cartella valida di Stardew Valley." -ForegroundColor Red
    Read-Host "Premi Invio per uscire..."
    exit 1
}

# Salva il percorso per usi futuri
Save-GamePath $gamePath

$saPresent = Check-StardewAccess $gamePath

while ($true) {
    $currentVer = Get-InstalledVersion $gamePath
    $verString = if ($currentVer) { "v$currentVer" } else { "Non installato" }
    
    Clear-Host
    Write-Host "========================================="
    Write-Host "   StardewNavigator Mod Installer v1.0   "
    Write-Host "========================================="
    Write-Host "Percorso di gioco: $gamePath"
    Write-Host "Versione corrente: $verString"
    if ($saPresent) {
        Write-Host "Stardew Access:    Rilevato (Integrazione Screen Reader attiva)" -ForegroundColor Green
    } else {
        Write-Host "Stardew Access:    Non rilevato (Fallback messaggi HUD attivo)" -ForegroundColor Cyan
    }
    Write-Host "-----------------------------------------"
    Write-Host "  [1] Installa StardewNavigator (Ultima versione)"
    Write-Host "  [2] Aggiorna StardewNavigator"
    Write-Host "  [3] Disinstalla StardewNavigator"
    Write-Host "  [4] Verifica aggiornamenti disponibili"
    Write-Host "  [5] Esci"
    Write-Host "-----------------------------------------"
    
    $choice = Read-Host "Seleziona un'opzione [1-5]"
    
    switch ($choice) {
        "1" {
            Write-Host "`nRecupero informazioni ultima release..."
            $release = Get-LatestRelease
            if ($release) {
                $latestVer = $release.tag_name
                $zipAsset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1
                if (-not $zipAsset) {
                    Write-Host "Errore: Nessun file .zip trovato nella release GitHub." -ForegroundColor Red
                    Read-Host "Premi Invio per continuare..."
                    break
                }
                
                $zipUrl = $zipAsset.browser_download_url
                Write-Host "Trovata versione $latestVer."
                
                $tempZip = Join-Path $PSScriptRoot "temp_release.zip"
                $tempExtract = Join-Path $PSScriptRoot "temp_extract"
                
                Write-Host "Download in corso..." -ForegroundColor Yellow
                Invoke-WebRequest -Uri $zipUrl -OutFile $tempZip
                
                if (Test-Path $tempExtract) { Remove-Item -Recurse -Force $tempExtract }
                New-Item -ItemType Directory -Path $tempExtract | Out-Null
                
                Write-Host "Estrazione file..." -ForegroundColor Yellow
                Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
                
                $modFolderSource = Join-Path $tempExtract "StardewNavigator"
                if (-not (Test-Path $modFolderSource)) {
                    $modFolderSource = $tempExtract
                }
                
                $targetFolder = Join-Path $gamePath "Mods\StardewNavigator"
                if (Test-Path $targetFolder) { Remove-Item -Recurse -Force $targetFolder }
                New-Item -ItemType Directory -Path $targetFolder | Out-Null
                
                Copy-Item -Path "$modFolderSource\*" -Destination $targetFolder -Recurse -Force
                
                Remove-Item -Force $tempZip
                Remove-Item -Recurse -Force $tempExtract
                
                Write-Host "Installazione completata con successo! Versione: $latestVer" -ForegroundColor Green
            }
            Read-Host "Premi Invio per continuare..."
        }
        
        "2" {
            Write-Host "`nVerifica aggiornamenti..."
            if (-not $currentVer) {
                Write-Host "Il mod non e installato. Usa l'opzione 1 per installarlo." -ForegroundColor Yellow
            } else {
                $release = Get-LatestRelease
                if ($release) {
                    $latestVer = $release.tag_name.TrimStart('v')
                    if ([System.Version]$latestVer -gt [System.Version]$currentVer) {
                        Write-Host "Nuova versione disponibile: v$latestVer (Corrente: v$currentVer)" -ForegroundColor Green
                        $confirm = Read-Host "Vuoi aggiornare ora? (s/n)"
                        if ($confirm -eq "s" -or $confirm -eq "y" -or $confirm -eq "si") {
                            # Riesegue la logica dell'opzione 1
                            $zipAsset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1
                            if ($zipAsset) {
                                $zipUrl = $zipAsset.browser_download_url
                                $tempZip = Join-Path $PSScriptRoot "temp_release.zip"
                                $tempExtract = Join-Path $PSScriptRoot "temp_extract"
                                Write-Host "Download in corso..." -ForegroundColor Yellow
                                Invoke-WebRequest -Uri $zipUrl -OutFile $tempZip
                                if (Test-Path $tempExtract) { Remove-Item -Recurse -Force $tempExtract }
                                New-Item -ItemType Directory -Path $tempExtract | Out-Null
                                Write-Host "Estrazione file..." -ForegroundColor Yellow
                                Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
                                $modFolderSource = Join-Path $tempExtract "StardewNavigator"
                                if (-not (Test-Path $modFolderSource)) { $modFolderSource = $tempExtract }
                                $targetFolder = Join-Path $gamePath "Mods\StardewNavigator"
                                if (Test-Path $targetFolder) { Remove-Item -Recurse -Force $targetFolder }
                                New-Item -ItemType Directory -Path $targetFolder | Out-Null
                                Copy-Item -Path "$modFolderSource\*" -Destination $targetFolder -Recurse -Force
                                Remove-Item -Force $tempZip
                                Remove-Item -Recurse -Force $tempExtract
                                Write-Host "Aggiornamento completato con successo alla versione v$latestVer!" -ForegroundColor Green
                            }
                        }
                    } else {
                        Write-Host "Il mod e gia aggiornato all'ultima versione." -ForegroundColor Green
                    }
                }
            }
            Read-Host "Premi Invio per continuare..."
        }
        
        "3" {
            if (-not $currentVer) {
                Write-Host "`nIl mod non e installato." -ForegroundColor Yellow
            } else {
                $confirm = Read-Host "`nSei sicuro di voler disinstallare StardewNavigator? (s/n)"
                if ($confirm -eq "s" -or $confirm -eq "y" -or $confirm -eq "si") {
                    $targetFolder = Join-Path $gamePath "Mods\StardewNavigator"
                    if (Test-Path $targetFolder) {
                        Remove-Item -Recurse -Force $targetFolder
                        Write-Host "StardewNavigator disinstallato con successo." -ForegroundColor Green
                    } else {
                        Write-Host "Errore: Cartella non trovata." -ForegroundColor Red
                    }
                } else {
                    Write-Host "Disinstallazione annullata."
                }
            }
            Read-Host "Premi Invio per continuare..."
        }
        
        "4" {
            Write-Host "`nVerifica aggiornamenti..."
            $release = Get-LatestRelease
            if ($release) {
                $latestVer = $release.tag_name.TrimStart('v')
                if ($currentVer) {
                    if ([System.Version]$latestVer -gt [System.Version]$currentVer) {
                        Write-Host "Aggiornamento disponibile! v$latestVer e disponibile (Versione installata: v$currentVer)." -ForegroundColor Green
                    } else {
                        Write-Host "Nessun aggiornamento disponibile. Sei all'ultima versione (v$currentVer)." -ForegroundColor Green
                    }
                } else {
                    Write-Host "Il mod non e installato. Ultima versione disponibile: v$latestVer." -ForegroundColor Yellow
                }
            }
            Read-Host "Premi Invio per continuare..."
        }
        
        "5" {
            exit 0
        }
    }
}
