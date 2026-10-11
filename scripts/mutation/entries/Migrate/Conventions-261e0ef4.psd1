# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '(together is null || StyleBro.Analyzers.Ordering.MemberOrdering.IsSorted(type, together) != true)'
    Replace = 'true'
    Tests = 'MigrationTests.Conventions_CountMemberOrder'
}
