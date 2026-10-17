# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'is { Count: > 0 } lines'
    Replace = 'is { Count: > 999 } lines'
    Tests = 'MigrationTests.Init_RunAgain_KeepsTheConventionsItWrote'
}
