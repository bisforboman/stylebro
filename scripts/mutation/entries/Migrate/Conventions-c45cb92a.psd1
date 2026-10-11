# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '|| Value(silent) == convention.Default)'
    Replace = '|| true)'
    Tests = 'MigrationTests.Init_SilentBraces_FollowTheCode'
}
