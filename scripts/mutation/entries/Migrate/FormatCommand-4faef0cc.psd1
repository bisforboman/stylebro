# Trial 28: only a framework that compiles the same code stands in
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& !Ran(project, a.Framework) && key(project, a.Framework) == wanted)'
    Replace = '&& !Ran(project, a.Framework))'
    Tests = 'MigrationTests.Format_RunsAnotherMergedFramework_WhenTheChosenOneWasSkipped'
}
