# format: a project outside the solution goes to the run of its own framework
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'loading.Where(i => runs[i].Framework == own).Append(-1).First();'
    Replace = 'loading.Append(-1).First();'
    Tests = 'MigrationTests.Format_GivesAProjectOutsideTheSolution'
}
