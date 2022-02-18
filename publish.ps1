# Builds and packages binaries for publishing

$ErrorActionPreference = "Stop"

$VSPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
$MSBuild = "$VSPath\MSBuild\Current\Bin\MSBuild.exe"
$dotnet = "$VSPath\dotnet\runtime\dotnet.exe"

function Check-Result {
    if ($LASTEXITCODE -ne 0) {
        $ErrorActionPreference = "Continue"
        Write-Error -Message "Failed with error $LASTEXITCODE"
        Exit $LASTEXITCODE
    }
}

if (Test-Path -Path .\publish) {
    Remove-Item -Recurse .\publish
}

if (git status -s) {
    Write-Error -Message "Commit changes before publishing"
}

$version = Select-String -Path .\Directory.Build.props -Pattern "<Version>(.*)\.0</Version>" | % { $_.Matches.Groups[1].value }
if (-not $version) {
    Write-Error -Message "Could not find version number"
}

& $MSBuild InfiniteVariantTool.sln -target:Rebuild -property:Configuration=Release
Check-Result

& $dotnet publish .\GUI\GUI.csproj --property:PublishProfile=.\GUI\Properties\PublishProfiles\SelfContained.pubxml
Check-Result

& $dotnet publish .\GUI\GUI.csproj --property:PublishProfile=.\GUI\Properties\PublishProfiles\FrameworkDependent.pubxml
Check-Result

& $dotnet publish .\CLI\CLI.csproj --property:PublishProfile=.\CLI\Properties\PublishProfiles\SelfContained.pubxml
Check-Result

& $dotnet publish .\CLI\CLI.csproj --property:PublishProfile=.\CLI\Properties\PublishProfiles\FrameworkDependent.pubxml
Check-Result

Remove-Item .\publish -Recurse -Include *.pdb
$readme = ".\publish\self_contained\readme.txt"
Copy-Item README.md $readme
"v" + $version + "`r`n`r`n" + (Get-Content $readme -Raw) | Out-File $readme
Copy-Item $readme .\publish\framework_dependent\

$versioned_name = "InfiniteVariantTool_v$version"
Compress-Archive .\publish\self_contained\* ".\publish\$($versioned_name)_selfcontained.zip"
Write-Output ""
Write-Host "Success: .\publish\$($versioned_name)_selfcontained.zip" -ForegroundColor Green
Compress-Archive .\publish\framework_dependent\* ".\publish\$($versioned_name).zip"
Write-Host "Success: .\publish\$($versioned_name).zip" -ForegroundColor Green
