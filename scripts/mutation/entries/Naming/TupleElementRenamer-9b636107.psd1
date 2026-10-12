# BRO1311: a tuple element in a private member's signature isn't seen by other projects.
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'return symbol is null || CamelCaseRenamer.IsVisibleToOtherProjects(symbol);'
    Replace = 'return true;'
    Tests = 'KeptFindingsTests'
}
