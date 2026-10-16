# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'is { } severity ? Silent(severity) :'
    Replace = 'is { } severity ? false :'
    Tests = 'MigrationTests.Init_AKeyWhoseIdeRuleIsSilent_IsNotEnforced'
}
