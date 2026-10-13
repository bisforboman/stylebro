# BRO1311: a member use in code the run does not load keeps the rename.
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '.OutsideMembers.TryGetValue(oldName,'
    Replace = '.OutsideMembers.TryGetValue(oldName.ToUpperInvariant(),'
    Tests = 'FullyQualifiedName~ANamespaceOrTupleElementAProjectThisRunDoesntLoadUses_IsKept'
}
