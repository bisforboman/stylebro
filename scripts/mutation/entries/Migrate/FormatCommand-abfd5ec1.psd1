# Trial 23: a killed run's 'dotnet format' still running keeps its files swapped
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (record.Children ?? []).Any(c => IsRunning(c.Pid, c.Started))'
    Replace = ''
    Tests = 'MigrationTests.Format_ADotnetFormatThatOutlivesAKilledRun'
}
