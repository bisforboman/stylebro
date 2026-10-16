# BRO1311: a public operator's return type is a public signature (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = 'OperatorDeclarationSyntax o => o.ReturnType,'
    Replace = ''
    Tests = 'TupleElementNamingTests.PublicOperatorSignatures'
}
