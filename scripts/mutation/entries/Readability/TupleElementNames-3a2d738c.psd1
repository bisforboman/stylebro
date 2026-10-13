# BRO1124: 'x.Item1' stays when the element's name is the receiver's ('x.x')
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleElementNames.cs'
    Find = 'return receiver is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == element ? null : element;'
    Replace = 'return element;'
    Tests = 'TupleElementNameTests'
}
