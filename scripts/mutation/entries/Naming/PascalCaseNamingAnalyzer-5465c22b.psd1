# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = 'or "ExcludeFromCodeCoverageAttribute" or "PureAttribute")'
    Replace = 'or "ExcludeFromCodeCoverageAttribute" or "PureAttribute" or "FactAttribute")'
    Tests = 'AsyncSuffixTests'
}
