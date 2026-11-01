param([Parameter(Mandatory=$true)][string]$Evidence)
$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$exe=(Join-Path $projectRoot 'Builds/Tests/S3-G01/EchoStationAIProbe.exe')
$folder=[IO.Path]::GetFullPath((Join-Path $projectRoot $Evidence))
if(Test-Path -LiteralPath $folder){throw 'Fresh Windows evidence directory required'}
New-Item -ItemType Directory -Path $folder | Out-Null
$argsList=@('-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',('"'+(Join-Path $folder 'Player.log')+'"'),'--g01-root',('"'+$projectRoot+'"'),'--g01-output',('"'+$folder+'"'),'--g01-model','model_weight8_last.onnx','--g01-reference','WEIGHT8_ACC4_A01')
$process=Start-Process -FilePath $exe -ArgumentList $argsList -WorkingDirectory $projectRoot -PassThru -WindowStyle Normal
$timer=[Diagnostics.Stopwatch]::StartNew()
$samples=[Collections.Generic.List[object]]::new()
$timedOut=$false
while(-not $process.HasExited){
 $process.Refresh()
 $samples.Add([pscustomobject]@{seconds=$timer.Elapsed.TotalSeconds;privateBytes=$process.PrivateMemorySize64;workingSet=$process.WorkingSet64})
 if($timer.Elapsed.TotalSeconds -gt 120){
  if($process.Path -ne $exe){throw 'Owned process path mismatch; no kill performed'}
  $process.Kill();$timedOut=$true;break
 }
 Start-Sleep -Milliseconds 100
}
$process.WaitForExit()
$record=[pscustomobject]@{pid=$process.Id;exe=$exe;exitCode=$process.ExitCode;timedOut=$timedOut;seconds=$timer.Elapsed.TotalSeconds;samples=$samples.ToArray();maxPrivate=($samples | Measure-Object -Property privateBytes -Maximum).Maximum;maxWorkingSet=($samples | Measure-Object -Property workingSet -Maximum).Maximum}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $folder 'Process.json') -Encoding utf8
$record | Select-Object pid,exitCode,timedOut,seconds,maxPrivate,maxWorkingSet | Format-List
if($timedOut -or $process.ExitCode -ne 0){exit 1}
