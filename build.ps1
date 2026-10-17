param([switch]$Test, [switch]$Preview, [switch]$GuideFrames, [switch]$Clean, [string]$OutputDirectory = 'dist')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if ($Clean) { & (Join-Path $projectRoot 'clean.ps1') }
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Install Microsoft .NET Framework 4.8 to build MacroExecute.' }
$outputRoot = Join-Path $projectRoot $OutputDirectory
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$applicationPath = Join-Path $outputRoot 'MacroExecute.exe'
& $compilerPath /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 "/out:$applicationPath" "/win32manifest:$(Join-Path $projectRoot 'app.manifest')" "/win32icon:$(Join-Path $projectRoot 'assets/MacroExecute.ico')" "/resource:$(Join-Path $projectRoot 'assets/MacroExecute.ico'),MacroExecute.AppIcon" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
function Invoke-HiddenCheck([string[]]$CheckArguments, [string]$CheckName) {
    $checkProcess = Start-Process -FilePath $applicationPath -ArgumentList $CheckArguments -WindowStyle Hidden -PassThru
    try {
        if (-not $checkProcess.WaitForExit(180000)) {
            $checkProcess.Kill()
            $checkProcess.WaitForExit()
            throw "$CheckName exceeded three minutes. See $outputRoot for diagnostics."
        }
        return $checkProcess.ExitCode
    } finally { $checkProcess.Dispose() }
}
if ($Test) {
    $reportPath = Join-Path $outputRoot 'test-results.txt'
    $testExit = Invoke-HiddenCheck @('--self-test', ('"' + $reportPath + '"')) 'Headless tests'
    Get-Content -LiteralPath $reportPath
    if ($testExit -ne 0) { throw "Self-tests failed; see $reportPath and $outputRoot/last-error.txt." }
}
if ($Preview) {
    $previewPath = Join-Path $outputRoot 'MacroExecute-preview.png'
    $previewExit = Invoke-HiddenCheck @('--smoke', ('"' + $previewPath + '"')) 'Headless previews'
    if ($previewExit -ne 0) { throw 'UI smoke test failed.' }
}
if ($GuideFrames) {
    $framesExit = Invoke-HiddenCheck @('--export-guide-frames', ('"' + $outputRoot + '"')) 'Guide animation frames'
    if ($framesExit -ne 0) { throw 'Guide frame export failed.' }
}
Write-Output "Built $applicationPath"
