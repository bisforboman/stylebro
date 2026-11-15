# Following StyleCop 1.3.177-beta (issue 338): every copy of a multi-targeted file gets the same BRO1104 fix
@{
    File = 'src/StyleBro.CodeFixes/Readability/DefaultValueConstructorCodeFixProvider.cs'
    Find = 'cancellationToken, version, numericIntPtr) is'
    Replace = 'cancellationToken, version) is'
    Tests = 'DefaultValueConstructorTests.NativeIntegers_InSeveralTargets'
}
