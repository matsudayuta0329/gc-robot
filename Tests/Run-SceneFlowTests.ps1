param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$validationRoot = Join-Path $repositoryRoot 'Temp/SceneFlowValidation'
New-Item -ItemType Directory -Force $validationRoot | Out-Null
Copy-Item "$repositoryRoot/Assets", "$repositoryRoot/ProjectSettings" "$validationRoot/" -Recurse -Force
New-Item -ItemType Directory -Force "$validationRoot/Packages", "$validationRoot/Assets/FlowTests" | Out-Null
$dependencies = [ordered]@{}
$manifest = Get-Content "$repositoryRoot/Packages/manifest.json" -Raw | ConvertFrom-Json
foreach ($property in $manifest.dependencies.PSObject.Properties) { $dependencies[$property.Name] = $property.Value }
foreach ($package in Get-ChildItem "$repositoryRoot/Library/PackageCache" -Directory) {
    $packageJson = Join-Path $package.FullName 'package.json'
    if (Test-Path $packageJson) {
        $name = (Get-Content $packageJson -Raw | ConvertFrom-Json).name
        $dependencies[$name] = 'file:' + $package.FullName.Replace('\', '/')
    }
}
@{dependencies=$dependencies} | ConvertTo-Json -Depth 5 | Set-Content "$validationRoot/Packages/manifest.json"
Copy-Item "$PSScriptRoot/SceneFlowTests.cs" "$validationRoot/Assets/FlowTests" -Force
Move-Item -LiteralPath "$validationRoot/Assets/PlayerInputAction.cs" -Destination "$validationRoot/Assets/Scripts/PlayerInputAction.cs" -Force
Move-Item -LiteralPath "$validationRoot/Assets/PlayerInputAction.cs.meta" -Destination "$validationRoot/Assets/Scripts/PlayerInputAction.cs.meta" -Force
'{"name":"GcRobot.Runtime","references":["Unity.InputSystem","Unity.TextMeshPro","Unity.ugui"]}' | Set-Content "$validationRoot/Assets/Scripts/GcRobot.Runtime.asmdef"
'{"name":"GcRobot.SceneTests","references":["GcRobot.Runtime","Unity.InputSystem","Unity.TextMeshPro","Unity.ugui"],"optionalUnityReferences":["TestAssemblies"]}' | Set-Content "$validationRoot/Assets/FlowTests/GcRobot.SceneTests.asmdef"
$resultsPath = "$repositoryRoot/Temp/scene-flow-results.xml"
if (Test-Path -LiteralPath $resultsPath) { Remove-Item -LiteralPath $resultsPath }
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $validationRoot + '"'), '-runTests',
    '-testPlatform', 'PlayMode', '-testFilter', 'SceneFlowTests', '-testResults', ('"' + $resultsPath + '"'),
    '-logFile', ('"' + "$repositoryRoot/Temp/scene-flow-tests.log" + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if (!(Test-Path -LiteralPath $resultsPath)) { throw 'Scene tests did not run. See Temp/scene-flow-tests.log.' }
[xml]$results = Get-Content -LiteralPath $resultsPath
$results.'test-run' | Select-Object result, total, passed, failed
if ($process.ExitCode -ne 0 -or $results.'test-run'.result -ne 'Passed') { throw 'Scene flow tests failed.' }
