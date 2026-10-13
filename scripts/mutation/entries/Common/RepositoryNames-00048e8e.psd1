# BRO1311: a lambda parameter of the element's name is not a member use.
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'IdentifierNameSyntax name => name.Parent is MemberAccessExpressionSyntax access ? access.Name == name : name.Parent is MemberBindingExpressionSyntax or NameColonSyntax,'
    Replace = 'IdentifierNameSyntax => true,'
    Tests = 'KeptFindingsTests.AnInternalTupleElement_IsKeptOnlyForMemberUsesInProjectsThatSeeIt'
}
