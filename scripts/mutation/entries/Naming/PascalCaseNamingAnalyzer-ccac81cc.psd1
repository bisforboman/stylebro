# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '|| current.GetAttributes().Any(a => a.AttributeClass?.Name == "ApiControllerAttribute")'
    Replace = ''
    Tests = 'AsyncSuffixTests'
}
