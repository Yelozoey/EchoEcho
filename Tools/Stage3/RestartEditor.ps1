param([Parameter(Mandatory=$true)][int]$EditorPid,
      [Parameter(Mandatory=$true)][string]$Evidence)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$evidenceRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot $Evidence))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Docs\Tests\Stage3')) + [IO.Path]::DirectorySeparatorChar
if (-not $evidenceRoot.StartsWith($allowedRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence outside Stage3 tests' }
$logPath = Join-Path $evidenceRoot 'Launcher.json'
if (Test-Path -LiteralPath $logPath) { throw 'Fresh restart evidence required' }
try {
    Wait-Process -Id $EditorPid -Timeout 120 -ErrorAction Stop
    $launchedEditor = Start-Process -FilePath 'E:\App\Unity\Unity Editors\Installs\6000.6.7f1\Editor\Unity.exe' -ArgumentList @('-projectPath',('"'+$projectRoot+'"'),'-force-d3d11') -WindowStyle Normal -PassThru
    @{ utc=[DateTime]::UtcNow.ToString('O'); oldPid=$EditorPid; newPid=$launchedEditor.Id; forcedKill=$false; graphicsArgument='-force-d3d11' } | ConvertTo-Json | Set-Content -LiteralPath $logPath -Encoding UTF8
} catch {
    @{ utc=[DateTime]::UtcNow.ToString('O'); oldPid=$EditorPid; error=$_.Exception.Message; forcedKill=$false } | ConvertTo-Json | Set-Content -LiteralPath $logPath -Encoding UTF8
    exit 1
}
