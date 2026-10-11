# For agents (2026-10-10): --json, format --files, the AGENTS.md section
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var runArgs = report is null ? args : args.Concat(new[] { "--report", report }).ToArray();'
    Replace = 'var runArgs = args;'
    Tests = 'MigrationTests.Json_Format'
}
