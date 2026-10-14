# IDE0042 counts as enforced through a category or bulk severity too (trial 19)
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = 'cancellationToken, options, "Style")'
    Replace = 'cancellationToken)'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
