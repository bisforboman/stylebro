# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'
    Find = '&& !DisabledCode.Mentions(context.Compilation, symbol.Name))'
    Replace = ')'
    Tests = 'PrefixNamingTests.NamesInDisabledCode_AreSkipped'
}
