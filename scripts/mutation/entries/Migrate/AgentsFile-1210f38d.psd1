# For agents (2026-10-10): --json, format --files, the AGENTS.md section
@{
    File = 'src/StyleBro.Migrate/AgentsFile.cs'
    Find = 'if (!requested)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.AgentsMd_'
}
