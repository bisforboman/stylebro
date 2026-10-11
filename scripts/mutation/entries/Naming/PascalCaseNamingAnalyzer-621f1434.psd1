# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '|| current.Name.EndsWith("Controller", System.StringComparison.Ordinal)'
    Replace = ''
    Tests = 'AsyncSuffixTests'
}
