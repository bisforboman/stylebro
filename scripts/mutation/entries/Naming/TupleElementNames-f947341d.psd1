# BRO1311: a public type's header (record parameters, base types) is a public signature (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = 'TypeDeclarationSyntax type => type,'
    Replace = ''
    Tests = 'TupleElementNamingTests.PublicOperatorSignatures'
}
