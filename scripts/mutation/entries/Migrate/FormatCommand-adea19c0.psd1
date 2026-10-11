# For agents (2026-10-10): --json, format --files, the AGENTS.md section
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)'
    Replace = 'false'
    Tests = 'MigrationTests.Files_'
}
