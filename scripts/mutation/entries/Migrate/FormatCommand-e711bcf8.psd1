# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(p => p.Alias != alias)'
    Replace = '.Where(p => false)'
    Tests = 'MigrationTests.Format_AProjectThatSpellsTheFrameworkAnotherWay'
}
