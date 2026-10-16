# Trial 25: never an empty --diagnostics list (it means every rule)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'is { Count: > 1 } ids && ids.FindIndex('
    Replace = 'is { } ids && ids.FindIndex('
    Tests = 'MigrationTests.Format_SaysWhichCompiledFilesAreMissing'
}
