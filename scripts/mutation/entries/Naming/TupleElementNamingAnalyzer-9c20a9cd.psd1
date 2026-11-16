# BRO1311: a literal's name in code excluded by #if keeps its name
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'
    Find = '                || DisabledCode.Mentions(context.Compilation, identifier.ValueText)'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_WhereIde1006IsSuppressed_OrInDisabledCode'
}
