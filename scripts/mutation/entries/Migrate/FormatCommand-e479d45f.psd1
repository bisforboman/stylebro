# format: a multi-targeted project outside the solution gets no run (trial 29)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var own = frameworks.Length == 1 ? NormalizeFramework(frameworks[0]) : null;'
    Replace = 'var own = NormalizeFramework(frameworks[0]);'
    Tests = 'MigrationTests.Format_GivesAProjectOutsideTheSolution'
}
