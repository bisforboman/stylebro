# Trial 58 (ExpressionTreeToString): a project sees a type the compiler needs ('RuntimeHelpers') in two referenced
# assemblies, CS1685, which warnings as errors turn into an error without a place in the code, untouched too. check --build
# read it as an error that stops the build before the compiler and said "Not verified: ... compiled nothing past their
# errors", though the compiler reports it while binding and the analyzers ran (the trial's old-engine build reported 329
# findings there). Here a library defines its own RuntimeHelpers.GetSubArray (as polyfill packages do) and the app slices.
@{
    Description = 'a type the compiler needs defined in two references (CS1685 with warnings as errors, untouched too)'
    BuildFails = 'CS1685: RuntimeHelpers is in Poly and in the framework, untouched too'
    Create = {
        param($root)
        New-Project $root 'src/Poly/Poly.csproj'
        Set-Text $root 'src/Poly/RuntimeHelpers.cs' @'
namespace System.Runtime.CompilerServices;

public static class RuntimeHelpers
{
    public static T[] GetSubArray<T>(T[] array, Range range) => array;
}
'@
        New-Project $root 'src/App/App.csproj' -Properties ([ordered]@{ OutputType = 'Exe' }) -References '../Poly/Poly.csproj'
        Set-Text $root 'src/App/Program.cs' "var values = new[] { 1, 2, 3 };`nSystem.Console.WriteLine(values[1..].Length);"
        Set-Messy $root 'src/App/Messy.cs' -Namespace 'App'
        New-Solution $root 'Fixture.slnx' 'src/Poly/Poly.csproj', 'src/App/App.csproj'
    }
    Tests = $false
}
