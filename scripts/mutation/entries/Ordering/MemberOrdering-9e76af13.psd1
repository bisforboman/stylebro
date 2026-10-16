# Trial 24 audit: stylebro_member_order_namespaces = false
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if ((!options.OrderNamespaces && container is not TypeDeclarationSyntax)'
    Replace = 'if ((false && container is not TypeDeclarationSyntax)'
    Tests = 'MemberOrderingTests.NamespaceMembers_AreLeftAlone_WhenTurnedOff'
}
