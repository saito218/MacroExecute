param([switch]$KeepRelease, [switch]$Dependencies)
$ErrorActionPreference = 'Stop'
$cleanupRoot = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
# Only disposable, generated directories inside this repository are eligible.
$generatedDirectories = @('bin', 'obj', 'playwright-report', 'test-results', '__pycache__')
if (-not $KeepRelease) { $generatedDirectories += 'dist' }
if ($Dependencies) { $generatedDirectories += 'node_modules' }
foreach ($directoryName in $generatedDirectories) {
    $cleanupTarget = [IO.Path]::GetFullPath((Join-Path $cleanupRoot $directoryName))
    if ([IO.Path]::GetDirectoryName($cleanupTarget) -ne $cleanupRoot) { throw "Unsafe cleanup target: $cleanupTarget" }
    if (Test-Path -LiteralPath $cleanupTarget) {
        $cleanupItem = Get-Item -LiteralPath $cleanupTarget -Force
        if (($cleanupItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Refusing to clean linked directory: $cleanupTarget" }
        Remove-Item -LiteralPath $cleanupTarget -Recurse -Force
        Write-Output "Removed $directoryName"
    }
}
