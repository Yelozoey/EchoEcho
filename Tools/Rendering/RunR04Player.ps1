param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$EvidenceSubdirectory = 'Player')
$ErrorActionPreference = 'Stop'
$r04Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$r04Executable = Join-Path $r04Root 'Builds/ModuleTests/S1-R04/EchoStation_R04.exe'
if (-not (Test-Path -LiteralPath $r04Executable)) { throw 'Build R04 Validation Player in Unity first.' }
$r04Output = Join-Path $r04Root "Docs/Tests/Stage1/Evidence/S1-R04/$EvidenceSubdirectory"
New-Item -ItemType Directory -Path $r04Output -Force | Out-Null
$r04Arguments = @('-batchmode','-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"{0}"' -f (Join-Path $r04Output 'Player.log')),'--r04-report',('"{0}"' -f (Join-Path $r04Output 'PlayerProbe.json')),'--r04-exit')
$r04Started = [DateTime]::UtcNow
$r04Process = Start-Process -FilePath $r04Executable -ArgumentList $r04Arguments -WindowStyle Hidden -PassThru
$r04TimedOut = -not $r04Process.WaitForExit(45000)
if ($r04TimedOut) { $r04Process.Kill(); $r04Process.WaitForExit() }
$r04Exit = [ordered]@{pid=$r04Process.Id;exitCode=$r04Process.ExitCode;timedOut=$r04TimedOut;
    startedUtc=$r04Started.ToString('O');endedUtc=[DateTime]::UtcNow.ToString('O');executable=$r04Executable;arguments=$r04Arguments}
$r04Exit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $r04Output 'Exit.json') -Encoding utf8
$r04Exit | ConvertTo-Json
if ($r04TimedOut -or $r04Process.ExitCode -ne 0) { exit 1 }
