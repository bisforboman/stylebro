# Trial 28: only a framework run for the project itself needs a stand-in
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'foreach (var instead in frameworks.Where(f => Ran(project, f)).Order(StringComparer.OrdinalIgnoreCase))'
    Replace = 'foreach (var instead in frameworks.Order(StringComparer.OrdinalIgnoreCase))'
    Tests = 'MigrationTests.Format_RunsAnotherMergedFramework_WhenTheChosenOneWasSkipped'
}
