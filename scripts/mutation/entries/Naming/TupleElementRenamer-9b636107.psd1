# BRO1311: a tuple element in a private member's signature isn't seen by other projects.
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'return member is null || CamelCaseRenamer.IsVisibleToOtherProjects(member);'
    Replace = 'return true;'
    Tests = 'KeptFindingsTests'
}
