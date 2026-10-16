# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = ': colon >= 0 && Silent(value.Substring(colon + 1)))'
    Replace = ': false)'
    Tests = 'MigrationTests.Init_ASilentKey_IsNotEnforced'
}
