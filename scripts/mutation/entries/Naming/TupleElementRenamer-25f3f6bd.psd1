# BRO1311: a partner the analyzer leaves (disabled code) keeps its group
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'newNames[name] = compilations.Any(c => c is not null && DisabledCode.Mentions(c, name)) ? null : partner;'
    Replace = 'newNames[name] = partner;'
    Tests = 'TupleElementNamingTests.ElementsNextToANameInDisabledCode'
}
