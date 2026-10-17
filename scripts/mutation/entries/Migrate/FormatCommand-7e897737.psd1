# Trial 28: unknown code has no stand-in
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (key(project, instead) is not { } wanted)'
    Replace = 'if (key(project, instead) is not { } wanted && false)'
    Tests = 'MigrationTests.Format_RunsAnotherMergedFramework_WhenTheChosenOneWasSkipped'
}
