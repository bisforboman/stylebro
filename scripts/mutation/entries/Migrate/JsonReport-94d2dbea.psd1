# For agents (2026-10-10): --json, format --files, the AGENTS.md section
@{
    File = 'src/StyleBro.Migrate/JsonReport.cs'
    Find = 'if (afterSetting)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Json_Init'
}
