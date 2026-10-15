# Explicit event implementations count as private, like StyleCop's SA1202 (trial 23, vs-streamjsonrpc)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'IndexerDeclarationSyntax indexer => indexer.ExplicitInterfaceSpecifier is not null,'
    Replace = 'IndexerDeclarationSyntax indexer => indexer.ExplicitInterfaceSpecifier is not null, EventDeclarationSyntax evt => evt.ExplicitInterfaceSpecifier is not null,'
    Tests = 'MemberOrderingTests.ExplicitEventImplementation_CountsAsPrivate_LikeStyleCop'
}
