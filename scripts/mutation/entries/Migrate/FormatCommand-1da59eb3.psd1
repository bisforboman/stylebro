# Trial 9, tool (2026-10-12): what a run's projects reference loads too
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'next.Push(reference);'
    Replace = ''
    Tests = 'MigrationTests.Format_ProjectsThatCompileTheSameFiles'
}
