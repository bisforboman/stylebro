# Following StyleCop 1.3.177-beta (issue 338): nint.Zero needs C# 11
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = 'compilation is CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp11 }'
    Replace = 'compilation is CSharpCompilation'
    Tests = 'DefaultValueConstructorTests.NativeIntegers_BecomeZeroOnNet7WithCSharp11_DefaultElsewhere'
}
