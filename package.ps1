param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot
)

Set-Location "$PSScriptRoot"

$DistDir = "$OutputDirectory/dist"
if ($NoArchive)
{
    $ZipWorkDir = "$OutputDirectory"
}
else
{
    $ZipWorkDir = "$DistDir/tmp"
}
$ZipRootDir = "$ZipWorkDir/BepInEx"
$ZipInnerDir = "$ZipRootDir/plugins/FunctionalDisplays/"
$BuildDir = "build/*"
$LicenseFile = "LICENSE"

New-Item "$ZipInnerDir" -ItemType Directory -Force
Copy-Item -Force -Path "$LicenseFile", "$BuildDir" -Destination "$ZipInnerDir"

if (!$NoArchive)
{
    $VERSION = (Select-String -Pattern '([0-9]+\.[0-9]+\.[0-9]+)' -Path Gauge/Gauge.cs).Matches.Value
    $FILE_NAME = "$DistDir/FunctionalDisplays_v$VERSION.zip"
    Compress-Archive -Update -CompressionLevel Fastest -Path "$ZipRootDir" -DestinationPath "$FILE_NAME"
}
