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
# Written here, in the layout 'dotnet new sln' and 'dotnet sln add' of SDK 10 gave (a solution folder per parent folder,
# none for the project's own), so a shape is the same repository whatever SDK creates it: on the runner image of
# 2026-11-11 'dotnet sln add' failed for a project another listed project references ("Solution folder 'src' already
# contains a project with the filename 'Lib.csproj'").
function New-Solution {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string[]]$Projects)
    $full = Join-Path $Root $Path
    $folder = Split-Path $full -Parent
    New-Item -ItemType Directory -Force $folder | Out-Null
    $entries = @(foreach ($project in $Projects) {
        $relative = [IO.Path]::GetRelativePath($folder, (Join-Path $Root $project)).Replace('\', '/')
        $name = [IO.Path]::GetFileNameWithoutExtension($relative)
        $parts = @($relative.Split('/'))
        $dirs = @($parts | Select-Object -SkipLast 1)
        if ($dirs.Count -and $dirs[-1] -eq $name) { $dirs = @($dirs | Select-Object -SkipLast 1) }
        if ($dirs.Count -gt 1 -or $dirs -contains '..') { throw "New-Solution writes one level of solution folders: $project in $Path." }
        [pscustomobject]@{ Path = $relative; Name = $name; Folder = "$($dirs -join '')" }
    })
    $folders = @($entries.Folder | Where-Object { $_ } | Select-Object -Unique)
    $lines = [Collections.Generic.List[string]]::new()
    if ([IO.Path]::GetExtension($Path) -eq '.slnx') {
        $lines.Add('<Solution>')
        foreach ($f in $folders) {
            $lines.Add("  <Folder Name=`"/$f/`">")
            $entries | Where-Object Folder -EQ $f | ForEach-Object { $lines.Add("    <Project Path=`"$($_.Path)`" />") }
            $lines.Add('  </Folder>')
        }
        $entries | Where-Object { -not $_.Folder } | ForEach-Object { $lines.Add("  <Project Path=`"$($_.Path)`" />") }
        $lines.Add('</Solution>')
        [IO.File]::WriteAllText($full, ($lines -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
        return
    }

    # Ids from the names, so the file is the same on every run.
    $id = { param($text) '{' + ([guid]::new([Security.Cryptography.MD5]::HashData([Text.Encoding]::UTF8.GetBytes($text)))).ToString().ToUpperInvariant() + '}' }
    $lines.Add('')
    $lines.Add('Microsoft Visual Studio Solution File, Format Version 12.00')
    $lines.Add('# Visual Studio Version 17')
    $lines.Add('VisualStudioVersion = 17.0.31903.59')
    $lines.Add('MinimumVisualStudioVersion = 10.0.40219.1')
    $written = @{}
    foreach ($e in $entries) {
        if ($e.Folder -and -not $written[$e.Folder]) {
            $written[$e.Folder] = $true
            $lines.Add("Project(`"{2150E333-8FDC-42A3-9474-1A3956D46DE8}`") = `"$($e.Folder)`", `"$($e.Folder)`", `"$(& $id "folder:$($e.Folder)")`"")
            $lines.Add('EndProject')
        }
        $type = if ($e.Path.EndsWith('.vbproj')) { '{F184B08F-C81C-45F6-A57F-5ABD9991F28F}' } else { '{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}' }
        $lines.Add("Project(`"$type`") = `"$($e.Name)`", `"$($e.Path.Replace('/', '\'))`", `"$(& $id $e.Path)`"")
        $lines.Add('EndProject')
    }
    $configurations = @(foreach ($c in 'Debug', 'Release') { foreach ($platform in 'Any CPU', 'x64', 'x86') { "$c|$platform" } })
    $lines.Add('Global')
    $lines.Add("`tGlobalSection(SolutionConfigurationPlatforms) = preSolution")
    $configurations | ForEach-Object { $lines.Add("`t`t$_ = $_") }
    $lines.Add("`tEndGlobalSection")
    $lines.Add("`tGlobalSection(ProjectConfigurationPlatforms) = postSolution")
    foreach ($e in $entries) {
        foreach ($c in $configurations) {
            $target = "$($c.Split('|')[0])|Any CPU"
            $lines.Add("`t`t$(& $id $e.Path).$c.ActiveCfg = $target")
            $lines.Add("`t`t$(& $id $e.Path).$c.Build.0 = $target")
        }
    }
    $lines.Add("`tEndGlobalSection")
    $lines.Add("`tGlobalSection(SolutionProperties) = preSolution")
    $lines.Add("`t`tHideSolutionNode = FALSE")
    $lines.Add("`tEndGlobalSection")
    if ($folders.Count) {
        $lines.Add("`tGlobalSection(NestedProjects) = preSolution")
        $entries | Where-Object Folder | ForEach-Object { $lines.Add("`t`t$(& $id $_.Path) = $(& $id "folder:$($_.Folder)")") }
        $lines.Add("`tEndGlobalSection")
    }
    $lines.Add('EndGlobal')
    [IO.File]::WriteAllText($full, ($lines -join "`n") + "`n", [Text.UTF8Encoding]::new($true))
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
