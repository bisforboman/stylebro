# Trial 33: no xUnit ids where xunit.analyzers 1.24+ is restored.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (ReferencesXunit(root) && XunitWithoutFixAll(root) is null)'
    Replace = 'if (ReferencesXunit(root))'
    Tests = 'MigrationTests.Format_LeavesTheXunitFixesOut'
}
