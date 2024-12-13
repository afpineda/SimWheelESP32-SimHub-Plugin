<############################################################################

.SYNOPSYS
    Compile and ZIP for release

.AUTHOR
    Ángel Fernández Pineda. Madrid. Spain. 2024.

.LICENSE
    Licensed under the EUPL

#############################################################################>

#setup
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$thisPath = Split-Path $($MyInvocation.MyCommand.Path) -parent
Set-Location $thisPath
$releaseFolder = "$thisPath/bin/Release"
$assemblyFile = Join-Path $releaseFolder "Afpineda.ESP32SimWheelPlugin.dll"
$pdbFile = Join-Path $releaseFolder "Afpineda.ESP32SimWheelPlugin.pdb"
$outputFile = "$thisPath/dist/Afpineda.ESP32SimWheelPlugin.zip"

# Build
Write-Information "Compiling..."
msbuild /P:configuration=release
if ($LASTEXITCODE -eq 0) {
    if (-not (Test-Path $assemblyFile)) {
        Write-Error "Assembly file not found ($assemblyFile)"
    }
    if (-not (Test-Path $pdbFile)) {
        Write-Error "PDB file not found ($pdbFile)"
    }
    Remove-Item $outputFile -Force -ErrorAction SilentlyContinue | Out-Null
    Compress-Archive -Path $assemblyFile, $pdbFile -DestinationPath $outputFile
}
else {
    Write-Error "Exiting due to compiler error."
}
