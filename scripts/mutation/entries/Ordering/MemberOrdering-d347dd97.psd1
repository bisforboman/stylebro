# Explicit interface events count as public (issue 338)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '            EventDeclarationSyntax evt => evt.ExplicitInterfaceSpecifier is not null,'
    Replace = ''
    Tests = 'MemberOrderingTests.ExplicitEventImplementation_CountsAsPublic'
}
