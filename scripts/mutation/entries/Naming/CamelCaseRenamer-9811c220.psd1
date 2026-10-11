# BRO1314 (Async suffix): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (symbols.Any(s => CamelCaseNamingAnalyzer.GetBaseMembers(s).Any(b => !renamed.Contains(b.OriginalDefinition.ToDisplayString()))))'
    Replace = 'if (false)'
    Tests = 'AsyncSuffixTests'
}
