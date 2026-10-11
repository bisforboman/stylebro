# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '|| DisabledCode.Mentions(context.Compilation, name)'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInDisabledCode_AreSkipped'
}
