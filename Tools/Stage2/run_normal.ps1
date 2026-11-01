param([Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Evidence)
$ErrorActionPreference='Stop'
$executable=(Resolve-Path -LiteralPath $Exe).Path
$folder=[IO.Path]::GetFullPath((Join-Path (Get-Location) $Evidence))
if(Test-Path -LiteralPath $folder){throw 'Fresh evidence directory required'}
New-Item -ItemType Directory -Path $folder | Out-Null
$log=Join-Path $folder 'Player.log'
$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path -Parent $executable) -WindowStyle Normal -PassThru
$clock=[Diagnostics.Stopwatch]::StartNew()
$seenWindow=$false
$requestedClose=$false
$timedOut=$false
while(-not $process.HasExited){
 $process.Refresh()
 if($process.MainWindowHandle -ne 0){$seenWindow=$true}
 if($clock.Elapsed.TotalSeconds -gt 25 -and $seenWindow -and -not $requestedClose){$requestedClose=$process.CloseMainWindow()}
 if($clock.Elapsed.TotalSeconds -gt 60){$timedOut=$true;Stop-Process -Id $process.Id;break}
 Start-Sleep -Seconds 2
}
$process.WaitForExit()
$errors=@(Select-String -LiteralPath $log -Pattern 'Exception|Crash!!!|ERROR: Shader' | ForEach-Object {$_.Line})
$result=[ordered]@{passed=($seenWindow -and $requestedClose -and -not $timedOut -and $process.ExitCode -eq 0 -and $errors.Count -eq 0);exe=$executable;sha256=(Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash;pid=$process.Id;exitCode=$process.ExitCode;seconds=$clock.Elapsed.TotalSeconds;seenWindow=$seenWindow;requestedClose=$requestedClose;timedOut=$timedOut;testArgumentsAbsent=$true;errors=$errors;evidenceScope='Normal uninstrumented window boot/liveness/clean close; gameplay proven separately by explicit CLI input operator'}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'Result.json') -Encoding utf8
$result | ConvertTo-Json -Depth 4
if(-not $result.passed){exit 1}
