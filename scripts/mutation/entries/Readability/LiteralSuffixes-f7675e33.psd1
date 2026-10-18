# Trial 31: no suffix in BRO1123s literal where BRO1122 is off
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = '&& Severities.IsOn(model.Compilation.Options, literal.SyntaxTree, DiagnosticIds.LiteralSuffix, cancellationToken)'
    Replace = ''
    Tests = 'TupleSyntaxTests.ConvertedLiterals'
}
