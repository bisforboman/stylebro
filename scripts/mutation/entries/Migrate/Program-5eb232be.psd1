# For agents (2026-10-10): --json, format --files, the AGENTS.md section
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'Console.SetOut(Console.Error);'
    Replace = ''
    Tests = 'MigrationTests.Json_Main'
}
