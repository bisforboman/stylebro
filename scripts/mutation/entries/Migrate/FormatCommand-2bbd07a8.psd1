# Backlog 073 (2026-10-18): xUnit fixes in format: the runners and xunit.assert bring no analyzers
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '"xunit(?:\.v3(?:\.mtp-[\w-]+)?|\.analyzers)?""'
    Replace = '"xunit[\w.-]*""'
    Tests = 'MigrationTests.Format_FixesXunitAssertions_WhereTheRepositoryReferencesXunit'
}
