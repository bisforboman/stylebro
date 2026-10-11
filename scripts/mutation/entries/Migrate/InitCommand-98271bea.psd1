# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'out var severity) && Silent(severity)'
    Replace = 'out var severity)'
    Tests = 'MigrationTests.Init_AKeyWhoseIdeRuleIsSilent_IsNotEnforced'
}
