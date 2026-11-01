param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$EvidenceSubdirectory = 'Player')
$ErrorActionPreference = 'Stop'
$r03Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$r03Executable = Join-Path $r03Root 'Builds/ModuleTests/S1-R03/EchoStation_R03.exe'
if (-not (Test-Path -LiteralPath $r03Executable)) { throw 'Build R03 Validation Player in Unity first.' }
$r03Output = Join-Path $r03Root "Docs/Tests/Stage1/Evidence/S1-R03/$EvidenceSubdirectory"
New-Item -ItemType Directory -Path $r03Output -Force | Out-Null
$r03Arguments = @('-batchmode','-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"{0}"' -f (Join-Path $r03Output 'Player.log')),'--r03-report',('"{0}"' -f (Join-Path $r03Output 'PlayerProbe.json')),'--r03-exit')
$r03Started = [DateTime]::UtcNow
$r03Process = Start-Process -FilePath $r03Executable -ArgumentList $r03Arguments -WindowStyle Hidden -PassThru
$r03TimedOut = -not $r03Process.WaitForExit(45000)
if ($r03TimedOut) { $r03Process.Kill(); $r03Process.WaitForExit() }
$r03Exit = [ordered]@{pid=$r03Process.Id;exitCode=$r03Process.ExitCode;timedOut=$r03TimedOut;
    startedUtc=$r03Started.ToString('O');endedUtc=[DateTime]::UtcNow.ToString('O');executable=$r03Executable;arguments=$r03Arguments}
$r03Exit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $r03Output 'Exit.json') -Encoding utf8
$r03Exit | ConvertTo-Json
if ($r03TimedOut -or $r03Process.ExitCode -ne 0) { exit 1 }
