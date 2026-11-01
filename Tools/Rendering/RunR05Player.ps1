param([switch]$Visible, [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$EvidenceSubdirectory = 'Player')
$ErrorActionPreference = 'Stop'
$r05Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$r05Executable = Join-Path $r05Root 'Builds/ModuleTests/S1-R05/EchoStation_R05.exe'
if (-not (Test-Path -LiteralPath $r05Executable)) { throw 'Build R05 Validation Player in Unity first.' }
$r05Output = Join-Path $r05Root "Docs/Tests/Stage1/Evidence/S1-R05/$EvidenceSubdirectory"
New-Item -ItemType Directory -Path $r05Output -Force | Out-Null
$r05Arguments = @('-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"{0}"' -f (Join-Path $r05Output 'Player.log')),'--r05-report',('"{0}"' -f (Join-Path $r05Output 'PlayerProbe.json')),'--r05-exit')
if (-not $Visible) { $r05Arguments = @('-batchmode') + $r05Arguments }
$r05WindowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$r05Started = [DateTime]::UtcNow
$r05Process = Start-Process -FilePath $r05Executable -ArgumentList $r05Arguments -WindowStyle $r05WindowStyle -PassThru
$r05TimedOut = -not $r05Process.WaitForExit(60000)
if ($r05TimedOut) { $r05Process.Kill(); $r05Process.WaitForExit() }
$r05Exit = [ordered]@{pid=$r05Process.Id;exitCode=$r05Process.ExitCode;timedOut=$r05TimedOut;
    startedUtc=$r05Started.ToString('O');endedUtc=[DateTime]::UtcNow.ToString('O');executable=$r05Executable;arguments=$r05Arguments}
$r05Exit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $r05Output 'Exit.json') -Encoding utf8
$r05Exit | ConvertTo-Json
if ($r05TimedOut -or $r05Process.ExitCode -ne 0) { exit 1 }
