# Trial 10, tool (2026-10-12): no project with StyleBro stops the run
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var none = without.Count > 0 && without.Count == checkedProjects.Count;'
    Replace = 'var none = false;'
    Tests = 'MigrationTests.Format_WithoutStyleBroRestored_'
}
