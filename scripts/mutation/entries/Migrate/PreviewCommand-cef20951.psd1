# Trial 23: only copies older than a day go
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'IsOwnCopy(folder) && DateTime.UtcNow - Directory.GetLastWriteTimeUtc(folder) > age'
    Replace = 'IsOwnCopy(folder)'
    Tests = 'MigrationTests.StaleCopies_OlderThanADay_AreDeleted'
}
