# The renamer keeps a nested type a derived 'new' type hides
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| symbol is INamedTypeSymbol { ContainingType: not null }) && await IsHiddenByNewMemberAsync'
    Replace = ') && await IsHiddenByNewMemberAsync'
    Tests = 'PascalCaseNamingTests.NewMembersHidingABaseMember_AreNotReported'
}
