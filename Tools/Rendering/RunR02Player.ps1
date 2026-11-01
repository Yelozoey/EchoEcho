param(
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$EvidenceSubdirectory = 'PlayerFinal',
    [switch]$BatchMode,
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$r02Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$r02Executable = Join-Path $r02Root 'Builds/ModuleTests/S1-R02/EchoStation_R02.exe'
if (-not (Test-Path -LiteralPath $r02Executable)) { throw 'Build R02 Validation Player in Unity first.' }
$r02Output = Join-Path $r02Root "Docs/Tests/Stage1/Evidence/S1-R02/$EvidenceSubdirectory"
New-Item -ItemType Directory -Path $r02Output -Force | Out-Null
$r02Report = Join-Path $r02Output 'PlayerProbe.json'
$r02Log = Join-Path $r02Output 'Player.log'
$r02Arguments = @('-force-d3d11', '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"{0}"' -f $r02Log), '--r02-report', ('"{0}"' -f $r02Report), '--r02-exit')
if ($BatchMode) { $r02Arguments += '-batchmode' }
$r02Started = [DateTime]::UtcNow
$r02WindowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$r02Process = Start-Process -FilePath $r02Executable -ArgumentList $r02Arguments -WindowStyle $r02WindowStyle -PassThru
$r02TimedOut = -not $r02Process.WaitForExit(45000)
if ($r02TimedOut) { $r02Process.Kill(); $r02Process.WaitForExit() }
$r02Exit = [ordered]@{ pid=$r02Process.Id; exitCode=$r02Process.ExitCode; timedOut=$r02TimedOut;
    startedUtc=$r02Started.ToString('O'); endedUtc=[DateTime]::UtcNow.ToString('O');
    executable=$r02Executable; visible=[bool]$Visible; arguments=$r02Arguments }
$r02Exit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $r02Output 'Exit.json') -Encoding utf8
$r02Exit | ConvertTo-Json
if ($r02TimedOut -or $r02Process.ExitCode -ne 0) { exit 1 }
