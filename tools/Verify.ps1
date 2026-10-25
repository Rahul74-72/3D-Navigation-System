param(
    [ValidateSet('Verify','Prepare','Windows','Android','Smoke')][string]$Action = 'Verify',
    [string]$Unity = 'C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'UnityProject'
$qa = Join-Path $root 'qa'
New-Item -ItemType Directory -Force -Path $qa | Out-Null
function Run-Editor([string]$options,[string]$name) {
    $arguments = "-batchmode -projectPath `"$project`" -logFile `"$(Join-Path $qa $name)`" $options"
    $p = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($p.ExitCode -ne 0) { throw "Unity failed ($($p.ExitCode)). See $name" }
}
switch ($Action) {
    'Prepare' { Run-Editor '-quit -executeMethod Campus.Editor.CampusBuild.Prepare' 'prepare.log' }
    'Verify' {
        Run-Editor '-quit -executeMethod Campus.Editor.CampusBuild.Prepare' 'prepare.log'
        Run-Editor '-quit -executeMethod Campus.Editor.CampusBuild.ValidateRoomPairs' 'room-pairs.log'
        Run-Editor "-runTests -testPlatform EditMode -testResults `"$(Join-Path $qa 'editmode.xml')`"" 'tests.log'
        [xml]$results = Get-Content (Join-Path $qa 'editmode.xml')
        if ($results.'test-run'.result -ne 'Passed') { throw 'Tests did not pass.' }
        $results.'test-run' | Select-Object result,total,passed,failed,skipped
        Import-Csv (Join-Path $qa 'route-audit.csv') | Group-Object status | Select-Object Name,Count
    }
    'Windows' { Run-Editor '-quit -executeMethod Campus.Editor.CampusBuild.PrepareAndBuildWindows' 'build-windows.log' }
    'Android' { Run-Editor '-quit -executeMethod Campus.Editor.CampusBuild.Android' 'build-android.log' }
    'Smoke' {
        $exe = Join-Path $root 'Builds/Windows/CampusNavigator.exe'
        $p = Start-Process -FilePath $exe -ArgumentList "--campus-smoke -screen-fullscreen 0 -screen-width 1440 -screen-height 900 -logFile `"$(Join-Path $qa 'player.log')`"" -WindowStyle Hidden -PassThru -Wait
        if ($p.ExitCode -ne 0) { throw "Player smoke failed ($($p.ExitCode))." }
        Get-Content (Join-Path $qa 'runtime-smoke.txt')
    }
}
