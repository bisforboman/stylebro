# Following StyleCop 1.3.177-beta (issue 338): only an interpolated string without content is empty
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = 'c is InterpolatedStringTextSyntax { TextToken.ValueText.Length: 0 })'
    Replace = 'c is InterpolatedStringTextSyntax)'
    Tests = 'EmptyStringTests.OtherLiterals_NoDiagnostic'
}
