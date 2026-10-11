# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '? type.Arity <= 1 && ns.ToDisplayString() == "System.Threading.Tasks"'
    Replace = '? true'
    Tests = 'AsyncSuffixTests'
}
