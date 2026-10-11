# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'
    Find = '&& !DisabledCode.Mentions(symbol.ContainingSymbol, symbol.Name, context.CancellationToken))'
    Replace = ')'
    Tests = 'PrefixNamingTests.NamesInDisabledCode_AreSkipped'
}
