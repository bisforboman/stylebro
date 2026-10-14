#!/usr/bin/env pwsh
# Writes data/inventory-<version>.csv: every diagnostic in the given StyleCop.Analyzers versions, with title,
# category, default state and whether StyleCop ships a code fix. Restores the packages into the NuGet cache first.
# -Package StyleSharp.Analyzers writes data/inventory-stylesharp-<version>.csv the same way (StyleSharp, a StyleCop
# successor with SST ids, which stylebro-migrate reads like StyleCop: New-StyleSharpMapping.ps1).
param([string[]]$Versions = @('1.0.2', '1.1.118', '1.2.0-beta.556'), [string]$Package = 'StyleCop.Analyzers')
$ErrorActionPreference = 'Stop'

$data = Join-Path $PSScriptRoot 'data'
$tool = Join-Path $PSScriptRoot 'Inventory'
New-Item $data -ItemType Directory -Force | Out-Null
dotnet build $tool --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Building the inventory tool failed.' }

foreach ($version in $Versions) {
    # Restore in a throwaway project outside the repo, so central package management doesn't apply.
    $temp = Join-Path ([IO.Path]::GetTempPath()) "stylebro-inventory-$Package-$version"
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
    New-Item $temp -ItemType Directory | Out-Null
    Set-Content (Join-Path $temp 'p.csproj') @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include="$Package" Version="$version" /></ItemGroup>
</Project>
"@
    dotnet restore $temp -v q
    if ($LASTEXITCODE -ne 0) { throw "Restoring $Package $version failed." }

    # The 1.2 betas are a metapackage over StyleCop.Analyzers.Unstable, so find whichever package holds the DLL.
    # StyleSharp packs one DLL per Roslyn version: the 4.8 one, the version StyleBro builds against.
    $dll = "$Package.dll"
    $assets = Get-Content (Join-Path $temp 'obj/project.assets.json') -Raw | ConvertFrom-Json
    $packages = @($assets.packageFolders.PSObject.Properties.Name)[0]
    $library = $assets.libraries.PSObject.Properties |
        Where-Object { $_.Value.files -contains "analyzers/dotnet/cs/$dll" -or $_.Value.files -contains "analyzers/dotnet/roslyn4.8/cs/$dll" } |
        Select-Object -First 1
    if (-not $library) { throw "No $dll found for $version." }
    $slot = if ($library.Value.files -contains "analyzers/dotnet/cs/$dll") { 'analyzers/dotnet/cs' } else { 'analyzers/dotnet/roslyn4.8/cs' }
    $dir = Join-Path $packages (Join-Path $library.Value.path $slot)

    $name = if ($Package -eq 'StyleSharp.Analyzers') { "inventory-stylesharp-$version.csv" } else { "inventory-$version.csv" }
    dotnet run --project $tool --no-build -- $dir (Join-Path $data $name)
    Remove-Item $temp -Recurse -Force
}
