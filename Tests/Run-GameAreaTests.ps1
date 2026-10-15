param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe',
    [switch]$GenerateTemplates
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$validationRoot = Join-Path $repositoryRoot 'Temp/GameAreaValidation'
foreach ($folder in @('Runtime', 'Tests', 'Editor')) {
    $target = [IO.Path]::GetFullPath((Join-Path $validationRoot "Assets/$folder"))
    $allowed = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'Temp/GameAreaValidation/Assets')) + [IO.Path]::DirectorySeparatorChar
    if (!$target.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid validation directory.' }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
}
New-Item -ItemType Directory -Force "$validationRoot/Assets/Runtime", "$validationRoot/Assets/Tests", "$validationRoot/Packages", "$validationRoot/ProjectSettings" | Out-Null
Copy-Item "$repositoryRoot/Assets/Scripts/*" "$validationRoot/Assets/Runtime" -Recurse -Force
Copy-Item "$repositoryRoot/Assets/PlayerInputAction.cs" "$validationRoot/Assets/Runtime" -Force
Copy-Item "$repositoryRoot/ProjectSettings/ProjectVersion.txt" "$validationRoot/ProjectSettings" -Force
Copy-Item "$PSScriptRoot/GameAreaTests.cs" "$validationRoot/Assets/Tests" -Force
Copy-Item "$PSScriptRoot/UIFlowTests.cs" "$validationRoot/Assets/Tests" -Force
New-Item -ItemType Directory -Force "$validationRoot/Assets/Editor" | Out-Null
Copy-Item "$repositoryRoot/Assets/Editor/UIAnimationSetup.cs" "$validationRoot/Assets/Editor" -Force
if (Test-Path "$repositoryRoot/Assets/UIAnimations") {
    Copy-Item "$repositoryRoot/Assets/UIAnimations" "$validationRoot/Assets" -Recurse -Force
}
$dependencies = [ordered]@{}
$dependencies['com.unity.modules.animation'] = '1.0.0'
$dependencies['com.unity.modules.physics'] = '1.0.0'
foreach ($packageName in @('com.unity.inputsystem','com.unity.ugui','com.unity.test-framework','com.unity.ext.nunit')) {
    $packageDirectory = Get-ChildItem "$repositoryRoot/Library/PackageCache" -Directory | Where-Object Name -Like "$packageName@*" | Select-Object -First 1
    if ($null -eq $packageDirectory) { throw "Open the project in Unity first to cache $packageName." }
    $dependencies[$packageName] = 'file:' + $packageDirectory.FullName.Replace('\','/')
}
@{dependencies=$dependencies} | ConvertTo-Json -Depth 4 | Set-Content "$validationRoot/Packages/manifest.json"
'{"name":"GcRobot.Runtime","references":["Unity.InputSystem","Unity.TextMeshPro","Unity.ugui"]}' | Set-Content "$validationRoot/Assets/Runtime/GcRobot.Runtime.asmdef"
'{"name":"GcRobot.Tests","references":["GcRobot.Runtime","Unity.InputSystem","Unity.TextMeshPro","Unity.ugui"],"optionalUnityReferences":["TestAssemblies"]}' | Set-Content "$validationRoot/Assets/Tests/GcRobot.Tests.asmdef"
$resultsPath = "$repositoryRoot/Temp/game-area-results.xml"
if (Test-Path -LiteralPath $resultsPath) { Remove-Item -LiteralPath $resultsPath }
$unityArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $validationRoot + '"'), '-runTests', '-testPlatform', 'PlayMode', '-testResults', ('"' + $resultsPath + '"'), '-logFile', ('"' + "$repositoryRoot/Temp/game-area-tests.log" + '"'))
if ($GenerateTemplates) {
    $unityArguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $validationRoot + '"'),
        '-executeMethod', 'UIAnimationSetup.CreateTemplates', '-logFile', ('"' + "$repositoryRoot/Temp/ui-template-build.log" + '"'))
}
$process = Start-Process -FilePath $UnityEditor -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($GenerateTemplates) {
    if ($process.ExitCode -ne 0) { throw 'UI template generation failed. See Temp/ui-template-build.log.' }
    Copy-Item "$validationRoot/Assets/UIAnimations" "$repositoryRoot/Assets" -Recurse -Force
    Copy-Item "$validationRoot/Assets/UIAnimations.meta" "$repositoryRoot/Assets" -Force
    return
}
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $resultsPath)) { throw 'Unity tests failed to run. See Temp/game-area-tests.log.' }
[xml]$results = Get-Content -LiteralPath $resultsPath
$results.'test-run' | Select-Object result, total, passed, failed
if ($results.'test-run'.result -ne 'Passed') { throw 'PlayMode tests failed.' }
