# BRO1311: a public conversion operator's type is a public signature (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = 'ConversionOperatorDeclarationSyntax c => c.Type,'
    Replace = ''
    Tests = 'TupleElementNamingTests.PublicOperatorSignatures'
}
