# Only marked copies are deleted
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (IsOwnCopy(folder) && DateTime.UtcNow'
    Replace = 'if (DateTime.UtcNow'
    Tests = 'MigrationTests.StaleCopies_OlderThanADay_AreDeleted'
}
