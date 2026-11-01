param(
 [Parameter(Mandatory=$true)][string]$Exe,
 [Parameter(Mandatory=$true)][string]$Mode,
 [Parameter(Mandatory=$true)][string]$Evidence,
 [Parameter(Mandatory=$true)][string]$SaveDirectory,
 [int]$TimeoutSeconds=300
)
$ErrorActionPreference='Stop'
$executable=(Resolve-Path -LiteralPath $Exe).Path
$evidencePath=[IO.Path]::GetFullPath((Join-Path (Get-Location) $Evidence))
$slotDirectory=[IO.Path]::GetFullPath((Join-Path (Get-Location) $SaveDirectory))
if(Test-Path -LiteralPath $evidencePath){throw 'Fresh evidence directory required'}
New-Item -ItemType Directory -Path $evidencePath | Out-Null
$slotFile=Join-Path $slotDirectory 'slot1.json'
$before=if(Test-Path -LiteralPath $slotFile){(Get-FileHash -LiteralPath $slotFile -Algorithm SHA256).Hash}else{$null}
$report=Join-Path $evidencePath 'Result.json'
$log=Join-Path $evidencePath 'Player.log'
$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$log+'"'),'--s2-window-report',('"'+$report+'"'),'--s2-window-mode',$Mode,'--s2-window-save-dir',('"'+$slotDirectory+'"'))
if($Mode -eq 'performance'){$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$log+'"'),'--s2-performance-report',('"'+$report+'"'))}
$process=Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path -Parent $executable) -WindowStyle Normal -PassThru
$watch=[Diagnostics.Stopwatch]::StartNew()
$timedOut=$false
while(-not $process.HasExited){
 if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$timedOut=$true;Stop-Process -Id $process.Id;break}
 Start-Sleep -Seconds 2
 $process.Refresh()
}
$process.WaitForExit()
$after=if(Test-Path -LiteralPath $slotFile){(Get-FileHash -LiteralPath $slotFile -Algorithm SHA256).Hash}else{$null}
$record=[ordered]@{exe=$executable;mode=$Mode;pid=$process.Id;exitCode=$process.ExitCode;timedOut=$timedOut;seconds=$watch.Elapsed.TotalSeconds;slot=$slotFile;sha256Before=$before;sha256After=$after;reportExists=(Test-Path -LiteralPath $report)}
$record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath 'Process.json') -Encoding utf8
$record | ConvertTo-Json
if($timedOut -or $process.ExitCode -ne 0 -or -not(Test-Path -LiteralPath $report)){exit 1}
$result=Get-Content -LiteralPath $report -Raw -Encoding utf8 | ConvertFrom-Json
if(-not $result.passed){exit 1}
