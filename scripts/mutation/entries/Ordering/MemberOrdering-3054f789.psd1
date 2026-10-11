# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (keys is null || keys.All(k => k.CompareTo(keys[0]) == 0))'
    Replace = 'if (keys is null)'
    Tests = 'MigrationTests.Conventions_CountMemberOrder'
}
