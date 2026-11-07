param([switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'Lucy.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Lucy.exe was not found next to this installer.' }
$name = 'Lucy - Masaüstü Dostun.lnk'
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) $name
$startupLink = Join-Path ([Environment]::GetFolderPath('Startup')) $name
$shell = New-Object -ComObject WScript.Shell
try {
    foreach ($entry in @(@{Path=$desktopLink;Arguments='--settings'}, @{Path=$startupLink;Arguments='--startup'})) {
        $link = $shell.CreateShortcut($entry.Path)
        if ((Test-Path -LiteralPath $entry.Path) -and $link.TargetPath -and $link.TargetPath -ne $exe) {
            throw ('An unrelated shortcut with the same name already exists: ' + $entry.Path)
        }
        $link.TargetPath = $exe
        $link.Arguments = $entry.Arguments
        $link.WorkingDirectory = $PSScriptRoot
        $link.Description = 'Lucy - Masauestundeki kucuk dostun'
        $link.IconLocation = $exe + ',0'
        $link.Save()
        [Runtime.InteropServices.Marshal]::FinalReleaseComObject($link) | Out-Null
    }
} finally { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) | Out-Null }
Write-Output ('Desktop: ' + $desktopLink)
Write-Output ('Startup: ' + $startupLink)
if (-not $NoLaunch) { Start-Process -FilePath $exe -WorkingDirectory $PSScriptRoot }
