param([switch]$Visible, [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$EvidenceSubdirectory = 'Player')
$ErrorActionPreference = 'Stop'
$w01Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$w01Executable = Join-Path $w01Root 'Builds/ModuleTests/S1-W01/EchoStation_W01.exe'
if (-not (Test-Path -LiteralPath $w01Executable)) { throw 'Build W01 Validation Player in Unity first.' }
$w01Output = Join-Path $w01Root "Docs/Tests/Stage1/Evidence/S1-W01/$EvidenceSubdirectory"
New-Item -ItemType Directory -Path $w01Output -Force | Out-Null
$w01Arguments = @('-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"{0}"' -f (Join-Path $w01Output 'Player.log')),'--w01-report',('"{0}"' -f (Join-Path $w01Output 'PlayerProbe.json')),'--w01-exit')
if (-not $Visible) { $w01Arguments = @('-batchmode') + $w01Arguments }
$w01WindowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$w01Started = [DateTime]::UtcNow
$w01Process = Start-Process -FilePath $w01Executable -ArgumentList $w01Arguments -WindowStyle $w01WindowStyle -PassThru
$w01TimedOut = -not $w01Process.WaitForExit(60000)
if ($w01TimedOut) { $w01Process.Kill(); $w01Process.WaitForExit() }
$w01Exit = [ordered]@{pid=$w01Process.Id;exitCode=$w01Process.ExitCode;timedOut=$w01TimedOut;
    startedUtc=$w01Started.ToString('O');endedUtc=[DateTime]::UtcNow.ToString('O');executable=$w01Executable;arguments=$w01Arguments}
$w01Exit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $w01Output 'Exit.json') -Encoding utf8
$w01Exit | ConvertTo-Json
if ($w01TimedOut -or $w01Process.ExitCode -ne 0) { exit 1 }
