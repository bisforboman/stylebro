#!/usr/bin/env pwsh
# Writes data/inventory-<version>.csv: every diagnostic in the given StyleCop.Analyzers versions, with title,
# category, default state and whether StyleCop ships a code fix. Restores the packages into the NuGet cache first.
param([string[]]$Versions = @('1.1.118', '1.2.0-beta.556'))
$ErrorActionPreference = 'Stop'

$data = Join-Path $PSScriptRoot 'data'
$tool = Join-Path $PSScriptRoot 'Inventory'
New-Item $data -ItemType Directory -Force | Out-Null
dotnet build $tool --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Building the inventory tool failed.' }

foreach ($version in $Versions) {
    # Restore in a throwaway project outside the repo, so central package management doesn't apply.
    $temp = Join-Path ([IO.Path]::GetTempPath()) "stylebro-inventory-$version"
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
    New-Item $temp -ItemType Directory | Out-Null
    Set-Content (Join-Path $temp 'p.csproj') @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="$version" /></ItemGroup>
</Project>
"@
    dotnet restore $temp -v q
    if ($LASTEXITCODE -ne 0) { throw "Restoring StyleCop.Analyzers $version failed." }

    # The 1.2 betas are a metapackage over StyleCop.Analyzers.Unstable, so find whichever package holds the DLL.
    $assets = Get-Content (Join-Path $temp 'obj/project.assets.json') -Raw | ConvertFrom-Json
    $packages = @($assets.packageFolders.PSObject.Properties.Name)[0]
    $library = $assets.libraries.PSObject.Properties |
        Where-Object { $_.Value.files -contains 'analyzers/dotnet/cs/StyleCop.Analyzers.dll' } | Select-Object -First 1
    if (-not $library) { throw "No StyleCop.Analyzers.dll found for $version." }
    $dir = Join-Path $packages (Join-Path $library.Value.path 'analyzers/dotnet/cs')

    dotnet run --project $tool --no-build -- $dir (Join-Path $data "inventory-$version.csv")
    Remove-Item $temp -Recurse -Force
}
