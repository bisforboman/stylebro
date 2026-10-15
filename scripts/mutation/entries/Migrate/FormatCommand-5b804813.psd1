# Trial 23: only filters written before this process started are leftovers
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (File.GetLastWriteTimeUtc(file) < started)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Format_ADotnetFormatThatOutlivesAKilledRun'
}
