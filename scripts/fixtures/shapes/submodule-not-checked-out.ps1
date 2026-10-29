# Trial 60 (khellang/Middleware cloned without --recursive): ProblemDetails compiles 'lib/AspNetCore/src/Shared/**/*.cs'
# from a git submodule whose folder is empty, so it didn't compile, and format and check said clean without a word. Format
# now names the submodule (Missing:) and leaves out IDE0005, as for compiled files that aren't there (owner's decision
# 2026-10-29). Here Lib compiles '../../lib/Shared/**/*.cs' from a submodule (a gitlink, so a clone has an empty folder).
@{
    Description = "a project compiles files from a git submodule that isn't checked out"
    BuildFails = "CS0103: Lib's code calls the submodule's Helper, which isn't checked out"
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj' -Extra "  <ItemGroup>`n    <Compile Include=`"../../lib/Shared/**/*.cs`" Link=`"Shared/%(RecursiveDir)%(Filename)%(Extension)`" />`n  </ItemGroup>"
        Set-Text $root 'src/Lib/Names.cs' "namespace Lib;`n`npublic static class Names`n{`n    public static string Name() => Shared.Helper.Name();`n}"
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Solution $root 'Fixture.slnx' 'src/Lib/Lib.csproj'
        Set-Text $root '.gitmodules' "[submodule `"lib/Shared`"]`n`tpath = lib/Shared`n`turl = https://example.invalid/shared.git"
        git -C $root update-index --add --cacheinfo "160000,1111111111111111111111111111111111111111,lib/Shared"
    }
    Assert = {
        param($formatted, $runs, $context)
        foreach ($engine in $runs.Keys) {
            foreach ($step in 'format', 'check') {
                $text = $runs[$engine].Steps[$step].Text
                if ($text -notmatch "Missing: the git submodule lib/Shared isn't checked out") { "$engine $step didn't name the submodule:`n$text" }
            }
        }
    }
    Tests = $false
}
