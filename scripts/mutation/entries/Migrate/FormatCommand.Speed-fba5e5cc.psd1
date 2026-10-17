# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = ' || !File.ReadAllText(assets).Contains("\"StyleBro.Analyzers/", StringComparison.OrdinalIgnoreCase))'
    Replace = ')'
    Tests = 'MigrationTests.Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent'
}
