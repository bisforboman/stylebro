# Trial 8, tool (2026-10-11): --diff stops up front for old-style projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'is { Count: > 0 } oldStyle)'
    Replace = 'is { Count: < 0 } oldStyle)'
    Tests = 'MigrationTests.OldStyleProjects_'
}
