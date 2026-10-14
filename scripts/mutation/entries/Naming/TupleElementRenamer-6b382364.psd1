# BRO1311: a partner with nothing to rename does not hold the group back
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'else if (reason?.Reason != KeptReason.NothingToRename)'
    Replace = 'else if (reason is not null)'
    Tests = 'TupleElementNamingTests.PartnersOnlyInPublicSignatures'
}
