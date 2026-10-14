# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '|| (symbol is not IMethodSymbol { MethodKind: MethodKind.LocalFunction } && DisabledCode.Mentions(context.Compilation, symbol.Name))'
    Replace = ''
    Tests = 'PascalCaseNamingTests.NamesInDisabledCode_AreSkipped'
}
