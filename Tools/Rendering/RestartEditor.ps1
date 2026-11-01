param([Parameter(Mandatory = $true)][int]$EditorPid)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$unityEditorPath = 'E:\App\Unity\Unity Editors\Installs\6000.6.7f1\Editor\Unity.exe'
$restartLog = Join-Path $projectRoot 'Docs\Tests\Stage1\Evidence\S1-R01\RestartLauncher.log'
try {
    Wait-Process -Id $EditorPid -Timeout 120 -ErrorAction Stop
    $launchedEditor = Start-Process -FilePath $unityEditorPath -ArgumentList @('-projectPath', ('"' + $projectRoot + '"'), '-force-d3d11') -WindowStyle Normal -PassThru
    [IO.File]::WriteAllText($restartLog, "Started Unity with -force-d3d11; PID=$($launchedEditor.Id)")
} catch {
    [IO.File]::WriteAllText($restartLog, $_.Exception.Message)
    exit 1
}
