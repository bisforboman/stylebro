# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (csharpier.Lines is not null && Conventions.IsCSharpiers(convention))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_WithCSharpier_TurnsItsLayoutRulesOff_AndKeepsTheRest'
}
