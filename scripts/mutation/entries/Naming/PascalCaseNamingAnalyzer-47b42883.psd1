# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '&& method.Name.IndexOf("Async", System.StringComparison.Ordinal) < 0'
    Replace = ''
    Tests = 'AsyncSuffixTests'
}
