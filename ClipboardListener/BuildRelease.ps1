
# BuildRelease.ps1
Set-Location -Path $PSScriptRoot

$AppName = "ClipboardListener" 

# Get contents of ./Releases/RELEASES text file
$releasesFile = ".\Releases\RELEASES"
$releasesContent = Get-Content -Path $releasesFile

# Each line looks like this: 5D4B7E7B5E3E067914E15D610948DB31BFA6F193 ClipboardListener-1.0.0-full.nupkg 2195790
# Extract the version numbers from each line
$largestVersion = $releasesContent 
    | Where-Object { $_ -match '-(\d+\.\d+\.\d+)-full\.nupkg' } 
    | ForEach-Object { $matches[1] }
    | Sort-Object { [version]$_ } -Descending 
    | Select-Object -First 1

# Increment the patch number by 1
$v = $largestVersion.Split('.')
$newVersion = "{0}.{1}.{2}" -f ([int]$v[0]), ([int]$v[1]), ([int]$v[2] + 1)

Write-Host "Building $AppName version $newVersion"
dotnet build -c Release
vpk pack -u $AppName -v $newVersion -p .\bin\Release\net48 -o .\Releases --icon .\corrected_app_icon.ico

Write-Host "Built $AppName version $newVersion"