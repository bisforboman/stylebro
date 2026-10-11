# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = 'if (current.Name is "Controller" or "ControllerBase" or "Hub"'
    Replace = 'if (false'
    Tests = 'AsyncSuffixTests'
}
