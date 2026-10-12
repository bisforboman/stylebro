# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'filters.All(f => IsFilterOf('
    Replace = 'filters.All(f => true || IsFilterOf('
    Tests = 'MigrationTests.ASolutionWithSeveralFiltersOfIt'
}
