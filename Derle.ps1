param([ValidatePattern("^[A-Za-z0-9._-]+\.exe$")][string]$OutputName = "Lucy.exe")
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler was not found.' }
$references = @(
    (Join-Path $framework 'WPF\PresentationFramework.dll'),
    (Join-Path $framework 'WPF\PresentationCore.dll'),
    (Join-Path $framework 'WPF\WindowsBase.dll'),
    (Join-Path $framework 'System.Xaml.dll'),
    'System.Runtime.Serialization.dll', 'System.Web.Extensions.dll',
    'System.Windows.Forms.dll', 'System.Drawing.dll', 'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll'
)
$arguments = @('/nologo', '/codepage:65001', '/target:winexe', '/optimize+',
    ('/out:' + (Join-Path $PSScriptRoot $OutputName)),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'src\app.manifest')))
foreach ($reference in $references) { $arguments += '/reference:' + $reference }
$icon = Join-Path $PSScriptRoot 'assets\lucy.ico'
if (Test-Path -LiteralPath $icon) { $arguments += '/win32icon:' + $icon }
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler $arguments
if ($LASTEXITCODE -ne 0) { throw 'Lucy build failed.' }
if (-not (Test-Path -LiteralPath $icon)) {
    $process = Start-Process -FilePath (Join-Path $PSScriptRoot $OutputName) -ArgumentList @('--make-icon', ('"' + $icon + '"')) -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $icon)) { throw 'Lucy icon could not be prepared.' }
    & $compiler ($arguments + @('/win32icon:' + $icon))
    if ($LASTEXITCODE -ne 0) { throw 'Lucy final build failed.' }
}
Write-Output ('Built: ' + (Join-Path $PSScriptRoot $OutputName))
