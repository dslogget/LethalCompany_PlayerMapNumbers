$zipName = Split-Path -Path (Get-Location) -Leaf
$zipNameFinal = $($zipName + ".zip")
$thisFile = Split-Path -Path $PSCommandPath -Leaf
Write-Output $zipName
Write-Output $PSCommandPath

Push-Location
Set-Location $zipName
dotnet build -c "Release" -o ..\build
Pop-Location
mkdir -Force "BepInEx/plugins"
Move-Item -Force "build/*.dll" "BepInEx/plugins" 

$items = "manifest.json","README.md","CHANGELOG.md","BepInEx"
if (Test-Path $zipNameFinal) {
    Remove-Item $zipNameFinal -verbose
}
Compress-Archive $items $zipNameFinal