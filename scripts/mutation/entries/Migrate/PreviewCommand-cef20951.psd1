# Trial 23: only copies older than a day go
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(folder) > age)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.StaleCopies_OlderThanADay_AreDeleted'
}
