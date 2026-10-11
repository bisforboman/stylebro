# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '&& !IsEventHandler(method)'
    Replace = ''
    Tests = 'AsyncSuffixTests'
}
