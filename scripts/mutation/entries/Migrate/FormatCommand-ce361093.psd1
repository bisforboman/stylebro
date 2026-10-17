# Trial 28: each stand-in is tried once
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.FirstOrDefault(f => tried.Add((project, f)));'
    Replace = '.FirstOrDefault();'
    Tests = 'MigrationTests.Format_RunsAnotherMergedFramework_WhenTheChosenOneWasSkipped'
}
