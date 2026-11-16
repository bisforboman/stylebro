# Following StyleCop 1.3.177-beta (issue 338): nint.Zero only when every target framework compiles it
@{
    File = 'src/StyleBro.CodeFixes/LinkedFileFixAllProvider.cs'
    Find = '|| !holds(compilation))'
    Replace = ')'
    Tests = 'DefaultValueConstructorTests.NativeIntegers_InSeveralTargets'
}
