# Helpers the fixture shapes (scripts/fixtures/shapes/*.ps1) build their tiny repositories with. Every path is relative
# to the fixture's root, which each helper takes first; files are written byte for byte (LF unless -Crlf, UTF-8 without a
# BOM unless -Bom, a final line break unless -NoFinalNewline), so a shape says exactly what a checkout holds.
$ErrorActionPreference = 'Stop'

function Set-Text {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Text,
        [switch]$Crlf,
        [switch]$Bom,
        [switch]$NoFinalNewline)
    $full = Join-Path $Root $Path
    New-Item -ItemType Directory -Force (Split-Path $full -Parent) | Out-Null
    $text = $Text.Replace("`r`n", "`n")
    if (-not $NoFinalNewline -and -not $text.EndsWith("`n")) { $text += "`n" }
    if ($NoFinalNewline) { $text = $text.TrimEnd("`n") }
    if ($Crlf) { $text = $text.Replace("`n", "`r`n") }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($text)
    if ($Bom) { $bytes = [byte[]](0xEF, 0xBB, 0xBF) + $bytes }
    [IO.File]::WriteAllBytes($full, $bytes)
}

# An SDK-style project. -Frameworks 'net10.0' or 'net8.0;net10.0'; -Properties and -Packages are hashtables (ordered for a
# stable file); -References are project paths relative to the new project; -Extra is more XML before </Project>.
function New-Project {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path,
        [string]$Frameworks = 'net10.0',
        [Collections.IDictionary]$Properties = @{},
        [Collections.IDictionary]$Packages = @{},
        [string[]]$References = @(),
        [string]$Extra = '',
        [string]$Sdk = 'Microsoft.NET.Sdk')
    $tfm = if ($Frameworks.Contains(';')) { "<TargetFrameworks>$Frameworks</TargetFrameworks>" } else { "<TargetFramework>$Frameworks</TargetFramework>" }
    $lines = @("<Project Sdk=`"$Sdk`">", '  <PropertyGroup>', "    $tfm")
    foreach ($key in $Properties.Keys) { $lines += "    <$key>$($Properties[$key])</$key>" }
    $lines += '  </PropertyGroup>'
    if ($Packages.Count -gt 0) {
        $lines += '  <ItemGroup>'
        foreach ($key in $Packages.Keys) { $lines += "    <PackageReference Include=`"$key`" Version=`"$($Packages[$key])`" />" }
        $lines += '  </ItemGroup>'
    }

    if ($References.Count -gt 0) {
        $lines += '  <ItemGroup>'
        foreach ($reference in $References) { $lines += "    <ProjectReference Include=`"$reference`" />" }
        $lines += '  </ItemGroup>'
    }

    if ($Extra) { $lines += $Extra.TrimEnd() }
    $lines += '</Project>'
    Set-Text $Root $Path ($lines -join "`n")
}

# An xUnit v2 test project (net10.0 unless -Frameworks says otherwise).
function New-TestProject {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path,
        [string]$Frameworks = 'net10.0',
        [string[]]$References = @(),
        [Collections.IDictionary]$Packages = [ordered]@{},
        [Collections.IDictionary]$Properties = [ordered]@{},
        [string]$Extra = '')
    $all = [ordered]@{ 'Microsoft.NET.Test.Sdk' = '17.11.1'; 'xunit' = '2.9.2'; 'xunit.runner.visualstudio' = '2.8.2' }
    foreach ($key in $Packages.Keys) { $all[$key] = $Packages[$key] }
    $props = [ordered]@{ IsPackable = 'false'; IsTestProject = 'true' }
    foreach ($key in $Properties.Keys) { $props[$key] = $Properties[$key] }
    New-Project $Root $Path -Frameworks $Frameworks -Properties $props -Packages $all -References $References -Extra $Extra
}

# A solution listing the projects (paths relative to the root). -Format sln or slnx; -Path may put it in a subfolder.
function New-Solution {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string[]]$Projects)
    $full = Join-Path $Root $Path
    $folder = Split-Path $full -Parent
    New-Item -ItemType Directory -Force $folder | Out-Null
    $format = [IO.Path]::GetExtension($Path).TrimStart('.')
    $name = [IO.Path]::GetFileNameWithoutExtension($Path)
    dotnet new sln -n $name -o $folder --format $format | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet new sln failed for $Path." }
    dotnet sln $full add @($Projects | ForEach-Object { Join-Path $Root $_ }) | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for $Path." }
}

# A C# file with a few findings the first run fixes (BRO1106, IDE0036, BRO1509, BRO1001): every shape has something to
# format, so a shape whose code isn't checked shows it.
function Set-Messy {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Path, [string]$Namespace = 'Fixture', [string]$Class = [IO.Path]::GetFileNameWithoutExtension($Path), [switch]$Crlf, [switch]$Bom)
    Set-Text $Root $Path -Crlf:$Crlf -Bom:$Bom @"
namespace $Namespace;

public class $Class
{
    static public int Twice(int x) { return x * 2; }

    public string Name = "";
}
"@
}

# Commits everything under the root (git init first if needed), with a fixed identity and date so the trees are the same
# on every run.
function Save-Commit {
    param([Parameter(Mandatory)][string]$Root, [string]$Message = 'fixture')
    if (-not (Test-Path (Join-Path $Root '.git'))) { git -C $Root init -q -b main | Out-Null }
    git -C $Root add -A | Out-Null
    $env:GIT_AUTHOR_DATE = $env:GIT_COMMITTER_DATE = '2026-01-01T00:00:00Z'
    try { git -C $Root -c user.name=fixture -c user.email=fixture@example.com -c core.autocrlf=false commit -q --allow-empty -m $Message | Out-Null }
    finally { Remove-Item Env:GIT_AUTHOR_DATE, Env:GIT_COMMITTER_DATE -ErrorAction SilentlyContinue }
    if ($LASTEXITCODE -ne 0) { throw "git commit failed in $Root." }
}

Export-ModuleMember -Function Set-Text, Set-Messy, New-Project, New-TestProject, New-Solution, Save-Commit
