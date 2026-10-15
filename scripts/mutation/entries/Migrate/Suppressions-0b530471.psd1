# SA1300 on a type or member gets no BRO1312
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = 'list.Parent is MemberDeclarationSyntax))'
    Replace = 'list.Parent is null))'
    Tests = 'MigrationTests.Suppressions_SA1300OnAMember_GetsNoNamespaceRule'
}
