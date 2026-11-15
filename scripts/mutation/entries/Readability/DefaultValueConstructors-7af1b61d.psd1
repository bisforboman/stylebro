# Following StyleCop 1.3.177-beta (issue 338): BRO1104 writes nint.Zero where it compiles
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = ' || IsNumericIntPtr(numericIntPtr, model)))'
    Replace = '))'
    Tests = 'DefaultValueConstructorTests.NativeIntegers_BecomeZeroOnNet7WithCSharp11_DefaultElsewhere'
}
