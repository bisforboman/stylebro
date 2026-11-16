# BRO1311: a literal's name where IDE1006 is suppressed keeps its name
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '&& !NamingRules.AllowsRename(context.Compilation, options, identifier.GetLocation(), owner, null, context.CancellationToken)))'
    Replace = '&& false))'
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhereIde1006IsSuppressed_OrInDisabledCode'
}
