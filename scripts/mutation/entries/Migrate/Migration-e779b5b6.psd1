# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(f => folders is null || folders.Any('
    Replace = '.Where(f => true || folders.Any('
    Tests = 'MigrationTests.Write_TurnsThePresetOff'
}
