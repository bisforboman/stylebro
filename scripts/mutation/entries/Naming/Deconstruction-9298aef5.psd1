# a tuple read through Item1/Item2 is not reported by IDE0042
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = 'is not IFieldSymbol { IsImplicitlyDeclared: false })'
    Replace = 'is not IFieldSymbol)'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
