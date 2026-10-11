# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = 'return method.MethodKind == MethodKind.Ordinary'
    Replace = 'return true'
    Tests = 'AsyncSuffixTests'
}
