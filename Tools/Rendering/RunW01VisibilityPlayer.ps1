param([switch]$Visible, [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$EvidenceSubdirectory = 'VisibilityPlayer')
$ErrorActionPreference = 'Stop'
$w01b3Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$w01b3Executable = Join-Path $w01b3Root 'Builds/ModuleTests/S1-W01-B3/EchoStation_W01B3.exe'
if (-not (Test-Path -LiteralPath $w01b3Executable)) { throw 'Build W01B3 Validation Player in Unity first.' }
$w01b3Output = Join-Path $w01b3Root "Docs/Tests/Stage1/Evidence/S1-W01/$EvidenceSubdirectory"
New-Item -ItemType Directory -Path $w01b3Output -Force | Out-Null
$w01b3Arguments = @('-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"{0}"' -f (Join-Path $w01b3Output 'Player.log')),'--w01b3-report',('"{0}"' -f (Join-Path $w01b3Output 'PlayerProbe.json')),'--w01b3-exit')
if (-not $Visible) { $w01b3Arguments = @('-batchmode') + $w01b3Arguments }
$w01b3WindowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$w01b3Started = [DateTime]::UtcNow
$w01b3Process = Start-Process -FilePath $w01b3Executable -ArgumentList $w01b3Arguments -WindowStyle $w01b3WindowStyle -PassThru
$w01b3TimedOut = -not $w01b3Process.WaitForExit(60000)
if ($w01b3TimedOut) { $w01b3Process.Kill(); $w01b3Process.WaitForExit() }
$w01b3Exit = [ordered]@{pid=$w01b3Process.Id;exitCode=$w01b3Process.ExitCode;timedOut=$w01b3TimedOut;
    startedUtc=$w01b3Started.ToString('O');endedUtc=[DateTime]::UtcNow.ToString('O');executable=$w01b3Executable;arguments=$w01b3Arguments}
$w01b3Exit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $w01b3Output 'Exit.json') -Encoding utf8
$w01b3Exit | ConvertTo-Json
if ($w01b3TimedOut -or $w01b3Process.ExitCode -ne 0) { exit 1 }
