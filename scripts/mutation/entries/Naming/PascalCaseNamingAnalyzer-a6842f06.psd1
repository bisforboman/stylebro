# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = 'newName = (casingOn ? newName ?? symbol.Name : symbol.Name) + "Async";'
    Replace = 'newName = (newName ?? symbol.Name) + "Async";'
    Tests = 'AsyncSuffixTests'
}
