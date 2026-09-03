#Requires -Version 5.1
<#
.SYNOPSIS
    Synchronizes metadata from backup LaunchBox data to current data.
.DESCRIPTION
    Matches games by romm_remote_path + romm_file_name custom fields and copies
    metadata fields from backup XML to current XML. Also copies images if a backup
    images directory exists.
#>

param(
    [string]$BackupDataPath = "D:\Jogos\LaunchBox\Data - Copia\Platforms",
    [string]$CurrentDataPath = "D:\Jogos\LaunchBox\Data\Platforms",
    [string]$BackupImagesPath = "D:\Jogos\LaunchBox\Data - Copia\Images",
    [string]$CurrentImagesPath = "D:\Jogos\LaunchBox\Images"
)

$ErrorActionPreference = 'Continue'

$MetadataFields = @(
    'Developer', 'Publisher', 'Notes', 'Genre', 'ReleaseDate', 'PlayMode',
    'Rating', 'MaxPlayers', 'ReleaseType', 'WikipediaURL', 'CommunityStarRating',
    'CommunityStarRatingTotalVotes', 'VideoUrl', 'DatabaseID', 'Series',
    'SortTitle', 'Region', 'Version'
)

$totalMatched = 0
$totalUpdated = 0
$totalAltsCopied = 0
$totalCustomFieldsCopied = 0
$totalImagesCopied = 0
$filesProcessed = 0
$errors = @()

function Get-XmlElement {
    param([System.Xml.XmlElement]$Parent, [string]$Name)
    $node = $Parent.GetElementsByTagName($Name)
    if ($node.Count -gt 0) { return $node[0] }
    return $null
}

function Build-GameLookup {
    param([System.Xml.XmlDocument]$Xml)
    $games = @($Xml.LaunchBox.Game)
    $customFields = @()
    $alternateNames = @()

    $cfAll = $Xml.LaunchBox.CustomField
    if ($cfAll -is [System.Xml.XmlElement]) { $customFields = @($cfAll) }
    elseif ($cfAll) { $customFields = @($cfAll) }

    $altAll = $Xml.LaunchBox.AlternateName
    if ($altAll -is [System.Xml.XmlElement]) { $alternateNames = @($altAll) }
    elseif ($altAll) { $alternateNames = @($altAll) }

    $cfByGameId = @{}
    foreach ($cf in $customFields) {
        $gid = $cf.GameID
        if (-not $cfByGameId.ContainsKey($gid)) { $cfByGameId[$gid] = @() }
        $cfByGameId[$gid] += $cf
    }

    $altByGameId = @{}
    foreach ($alt in $alternateNames) {
        $gid = $alt.GameID
        if (-not $altByGameId.ContainsKey($gid)) { $altByGameId[$gid] = @() }
        $altByGameId[$gid] += $alt
    }

    $keyToGame = @{}
    foreach ($g in $games) {
        $gid = $g.ID
        $remotePath = $null
        $fileName = $null
        if ($cfByGameId.ContainsKey($gid)) {
            foreach ($cf in $cfByGameId[$gid]) {
                if ($cf.Name -eq 'romm_remote_path') { $remotePath = $cf.Value }
                elseif ($cf.Name -eq 'romm_file_name') { $fileName = $cf.Value }
            }
        }
        if ($remotePath -and $fileName) {
            $keyToGame["$remotePath|$fileName"] = $g
        }
    }

    return @{
        Games = $games
        CustomFields = $customFields
        AlternateNames = $alternateNames
        CfByGameId = $cfByGameId
        AltByGameId = $altByGameId
        KeyToGame = $keyToGame
    }
}

function Sync-XmlElement {
    param(
        [System.Xml.XmlElement]$Target,
        [System.Xml.XmlElement]$Source,
        [string]$FieldName
    )
    $sourceNode = Get-XmlElement -Parent $Source -Name $FieldName
    if ($null -eq $sourceNode) { return $false }
    $targetNode = Get-XmlElement -Parent $Target -Name $FieldName
    $sourceValue = $sourceNode.InnerText
    if ($null -ne $targetNode) {
        if ($targetNode.InnerText -eq $sourceValue) { return $false }
        $targetNode.InnerText = $sourceValue
    } else {
        $newNode = $Target.OwnerDocument.CreateElement($FieldName)
        $newNode.InnerText = $sourceValue
        $Target.AppendChild($newNode) | Out-Null
    }
    return $true
}

function Sync-GameMetadata {
    param(
        [System.Xml.XmlElement]$CurrentGame,
        [System.Xml.XmlElement]$BackupGame,
        [xml]$CurrentXml,
        [hashtable]$BackupLookup,
        [hashtable]$CurrentLookup
    )

    $changed = $false
    $altsCopied = 0
    $nonRommCopied = 0

    foreach ($field in $MetadataFields) {
        if (Sync-XmlElement -Target $CurrentGame -Source $BackupGame -FieldName $field) {
            $changed = $true
        }
    }

    $backupGameId = $BackupGame.ID
    $currentGameId = $CurrentGame.ID

    $backupAlts = @()
    if ($BackupLookup.AltByGameId.ContainsKey($backupGameId)) {
        $backupAlts = @($BackupLookup.AltByGameId[$backupGameId])
    }
    $currentAlts = @()
    if ($CurrentLookup.AltByGameId.ContainsKey($currentGameId)) {
        $currentAlts = @($CurrentLookup.AltByGameId[$currentGameId])
    }

    foreach ($backupAlt in $backupAlts) {
        $exists = $false
        foreach ($currentAlt in $currentAlts) {
            if ($currentAlt.Name -eq $backupAlt.Name -and $currentAlt.Region -eq $backupAlt.Region) {
                $exists = $true
                break
            }
        }
        if (-not $exists) {
            $newAlt = $CurrentXml.CreateElement('AlternateName')
            $g = $CurrentXml.CreateElement('GameID'); $g.InnerText = $currentGameId; $newAlt.AppendChild($g) | Out-Null
            $n = $CurrentXml.CreateElement('Name'); $n.InnerText = $backupAlt.Name; $newAlt.AppendChild($n) | Out-Null
            $r = $CurrentXml.CreateElement('Region'); $r.InnerText = $backupAlt.Region; $newAlt.AppendChild($r) | Out-Null
            $CurrentXml.LaunchBox.AppendChild($newAlt) | Out-Null
            $altsCopied++
            $changed = $true
        }
    }

    $backupCfs = @()
    if ($BackupLookup.CfByGameId.ContainsKey($backupGameId)) {
        $backupCfs = @($BackupLookup.CfByGameId[$backupGameId])
    }
    $currentCfs = @()
    if ($CurrentLookup.CfByGameId.ContainsKey($currentGameId)) {
        $currentCfs = @($CurrentLookup.CfByGameId[$currentGameId])
    }

    foreach ($backupCf in $backupCfs) {
        if ($backupCf.Name -notlike 'romm_*') {
            $exists = $false
            foreach ($currentCf in $currentCfs) {
                if ($currentCf.Name -eq $backupCf.Name) {
                    $exists = $true
                    if ($currentCf.Value -ne $backupCf.Value) {
                        $currentCf.Value = $backupCf.Value
                        $changed = $true
                    }
                    break
                }
            }
            if (-not $exists) {
                $newCf = $CurrentXml.CreateElement('CustomField')
                $g = $CurrentXml.CreateElement('GameID'); $g.InnerText = $currentGameId; $newCf.AppendChild($g) | Out-Null
                $n = $CurrentXml.CreateElement('Name'); $n.InnerText = $backupCf.Name; $newCf.AppendChild($n) | Out-Null
                $v = $CurrentXml.CreateElement('Value'); $v.InnerText = $backupCf.Value; $newCf.AppendChild($v) | Out-Null
                $CurrentXml.LaunchBox.AppendChild($newCf) | Out-Null
                $nonRommCopied++
                $changed = $true
            }
        }
    }

    return @{ Changed = $changed; AltsCopied = $altsCopied; NonRommCfCopied = $nonRommCopied }
}

$backupFiles = Get-ChildItem -Path $BackupDataPath -Filter "RomM*.xml" -File
Write-Host "Found $($backupFiles.Count) RomM backup XML files" -ForegroundColor Cyan

foreach ($backupFile in $backupFiles) {
    $fileName = $backupFile.Name
    $currentFile = Join-Path $CurrentDataPath $fileName

    if (-not (Test-Path $currentFile)) {
        Write-Host "  SKIP (no current file): $fileName" -ForegroundColor Yellow
        continue
    }

    $filesProcessed++
    Write-Host "Processing: $fileName" -ForegroundColor White

    try {
        [xml]$backupXml = Get-Content -Path $backupFile.FullName -Encoding UTF8
        [xml]$currentXml = Get-Content -Path $currentFile -Encoding UTF8
    } catch {
        $errors += "XML parse error in ${fileName}: $_"
        Write-Host "  ERROR: Failed to parse XML - $_" -ForegroundColor Red
        continue
    }

    $backupLookup = Build-GameLookup -Xml $backupXml
    $currentLookup = Build-GameLookup -Xml $currentXml

    $fileMatched = 0
    $fileUpdated = 0
    $fileAlts = 0
    $fileCfs = 0

    foreach ($pair in $backupLookup.KeyToGame.GetEnumerator()) {
        if ($currentLookup.KeyToGame.ContainsKey($pair.Key)) {
            $fileMatched++

            $result = Sync-GameMetadata `
                -CurrentGame $currentLookup.KeyToGame[$pair.Key] `
                -BackupGame $pair.Value `
                -CurrentXml $currentXml `
                -BackupLookup $backupLookup `
                -CurrentLookup $currentLookup

            if ($result.Changed) { $fileUpdated++ }
            $fileAlts += $result.AltsCopied
            $fileCfs += $result.NonRommCfCopied
        }
    }

    if ($fileUpdated -gt 0) {
        try {
            $currentXml.Save($currentFile)
            Write-Host "  Saved: $fileName" -ForegroundColor Green
        } catch {
            $errors += "Save error for ${fileName}: $_"
            Write-Host "  ERROR saving: $_" -ForegroundColor Red
        }
    }

    $totalMatched += $fileMatched
    $totalUpdated += $fileUpdated
    $totalAltsCopied += $fileAlts
    $totalCustomFieldsCopied += $fileCfs
    Write-Host "  Matched: $fileMatched, Updated: $fileUpdated, Alts added: $fileAlts, CustomFields added: $fileCfs" -ForegroundColor DarkGray
}

if (Test-Path $BackupImagesPath) {
    Write-Host "`nProcessing images..." -ForegroundColor Cyan
    $backupImageDirs = Get-ChildItem -Path $BackupImagesPath -Directory -Filter "RomM*"

    foreach ($backupImgDir in $backupImageDirs) {
        $targetDir = Join-Path $CurrentImagesPath $backupImgDir.Name

        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }

        $imageFiles = Get-ChildItem -Path $backupImgDir.FullName -Recurse -File
        foreach ($imgFile in $imageFiles) {
            $relativePath = $imgFile.FullName.Substring($backupImgDir.FullName.Length)
            $targetFile = Join-Path $targetDir $relativePath

            if (-not (Test-Path $targetFile)) {
                $targetFileDir = Split-Path $targetFile -Parent
                if (-not (Test-Path $targetFileDir)) {
                    New-Item -ItemType Directory -Path $targetFileDir -Force | Out-Null
                }
                Copy-Item -Path $imgFile.FullName -Destination $targetFile -Force
                $totalImagesCopied++
            }
        }
    }
} else {
    Write-Host "`nNo backup images directory found at: $BackupImagesPath" -ForegroundColor Yellow
    Write-Host "Skipping image copy." -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SYNC COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Files processed:     $filesProcessed"
Write-Host "Games matched:       $totalMatched"
Write-Host "Games updated:       $totalUpdated"
Write-Host "AlternateNames added: $totalAltsCopied"
Write-Host "Non-romm CustomFields added: $totalCustomFieldsCopied"
Write-Host "Images copied:       $totalImagesCopied"

if ($errors.Count -gt 0) {
    Write-Host "`nErrors encountered:" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
}
