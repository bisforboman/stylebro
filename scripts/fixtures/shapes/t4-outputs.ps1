# Trials 28, 29, 30: files T4 templates write are generated code.
# - The .cs next to each .tt (DependentUpon, AutoGen) isn't checked or formatted.
# - Moq (trial 29): '<#@ output extension=".Generated.cs" #>' writes another file, next to a hand-written .cs of the same
#   base name; the hand-written one was left unformatted and the TWAE build failed after a clean verify.
@{
    Description = 'T4 outputs, one beside a hand-written file of the same base name'
    Assert = {
        param($repo)
        if ((Get-Content -Raw (Join-Path $repo 'src/Lib/Values.cs')) -notmatch 'static public') { 'the T4 output Values.cs was formatted' }
        if ((Get-Content -Raw (Join-Path $repo 'src/Lib/Returns.Generated.cs')) -notmatch 'static public') { 'the T4 output Returns.Generated.cs was formatted' }
        if ((Get-Content -Raw (Join-Path $repo 'src/Lib/Returns.cs')) -match 'static public') { 'the hand-written Returns.cs was not formatted' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj' -Extra @'
  <ItemGroup>
    <None Update="Values.tt" Generator="TextTemplatingFileGenerator" LastGenOutput="Values.cs" />
    <Compile Update="Values.cs" DesignTime="True" AutoGen="True" DependentUpon="Values.tt" />
    <None Update="Returns.tt" Generator="TextTemplatingFileGenerator" LastGenOutput="Returns.Generated.cs" />
    <Compile Update="Returns.Generated.cs" DesignTime="True" AutoGen="True" DependentUpon="Returns.tt" />
  </ItemGroup>
'@
        Set-Text $root 'src/Lib/Values.tt' "<#@ template language=`"C#`" #>`n<#@ output extension=`".cs`" #>`nnamespace Lib;`n"
        Set-Messy $root 'src/Lib/Values.cs' -Namespace 'Lib'
        Set-Text $root 'src/Lib/Returns.tt' "<#@ template language=`"C#`" #>`n<#@ output extension=`".Generated.cs`" #>`nnamespace Lib;`n"
        Set-Messy $root 'src/Lib/Returns.Generated.cs' -Namespace 'Lib' -Class 'ReturnsGenerated'
        Set-Messy $root 'src/Lib/Returns.cs' -Namespace 'Lib'
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj'
    }
    Tests = $false
}
