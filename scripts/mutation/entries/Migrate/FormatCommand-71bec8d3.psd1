# Trial 25: only old temporary files are deleted
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (File.GetLastWriteTimeUtc(file) < before)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Format_DeletesWhatAKilledRunLeftInTheTemporaryFolder'
}
