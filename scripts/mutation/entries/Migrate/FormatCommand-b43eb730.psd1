# Backlog 073 (2026-10-18): xUnit fixes in format
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (ReferencesXunit(root))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_FixesXunitAssertions_WhereTheRepositoryReferencesXunit'
}
