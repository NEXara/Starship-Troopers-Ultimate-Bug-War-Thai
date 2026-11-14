[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$InstallerPath,
 [string]$OutputPath = (Join-Path $PSScriptRoot '..\.build\UBW-Thai-Setup.exe'),
 [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
$expectedInstallerHash = '3d03db0682f3e332fe389f7f715397f1a701fd49f03cc9a4ed09fc3d9df9ec6d'
$inputInstaller = (Resolve-Path -LiteralPath $InstallerPath).Path
if ((Get-FileHash -LiteralPath $inputInstaller -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedInstallerHash) { throw 'Use the exact official 1.0.2 installer. Input hash does not match.' }
$taskTemp = Join-Path ([IO.Path]::GetTempPath()) ('UBWThai-build-' + [Guid]::NewGuid().ToString('N'))
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $compilerPath)) { throw '.NET Framework compiler not found.' }
try {
 [IO.Directory]::CreateDirectory($taskTemp) | Out-Null
 $assembly = [Reflection.Assembly]::LoadFile($inputInstaller)
 $resourceNames = @('payload.pak','payload.utoc','payload.ucas','TH.png','TH.ico','Credits','FONT-NOTICES','Orbitron-OFL','Oxanium-OFL','Noto-OFL','NotoThai-OFL')
 $compilerArgs = @('/nologo','/target:winexe','/optimize+','/platform:anycpu','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll')
 foreach ($resourceName in $resourceNames) {
  $stream = $assembly.GetManifestResourceStream($resourceName)
  if ($null -eq $stream) { throw ('Missing resource: ' + $resourceName) }
  $resourceFile = Join-Path $taskTemp $resourceName
  try { $destination = [IO.File]::Create($resourceFile); try { $stream.CopyTo($destination) } finally { $destination.Dispose() } } finally { $stream.Dispose() }
  $compilerArgs += ('/resource:' + $resourceFile + ',' + $resourceName)
 }
 $absoluteOutput = [IO.Path]::GetFullPath($OutputPath)
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($absoluteOutput)) | Out-Null
 if (Test-Path -LiteralPath $absoluteOutput) { throw 'Output already exists; choose a new output path.' }
 $compilerArgs += ('/win32icon:' + (Join-Path $taskTemp 'TH.ico'))
 $compilerArgs += ('/out:' + $absoluteOutput)
 foreach ($sourceName in @('Manager.cs','Manager.Tests.cs','AssemblyInfo.cs')) { $compilerArgs += (Join-Path $PSScriptRoot $sourceName) }
 & $compilerPath $compilerArgs
 if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
 if ($SelfTest) {
  $testProcess = Start-Process -FilePath $absoluteOutput -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
  if ($testProcess.ExitCode -ne 0) { throw 'Fixture self-test failed; inspect installer-test-result.json beside the output.' }
 }
 Write-Output ('Built: ' + $absoluteOutput)
} finally {
 $resolvedTaskTemp = [IO.Path]::GetFullPath($taskTemp)
 $resolvedTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
 if ($resolvedTaskTemp.StartsWith($resolvedTempRoot,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($resolvedTaskTemp).StartsWith('UBWThai-build-') -and (Test-Path -LiteralPath $resolvedTaskTemp)) { Remove-Item -LiteralPath $resolvedTaskTemp -Recurse -Force }
}
