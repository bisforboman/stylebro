# Trial 15 (recyclarr): a new file is a build step's, not the format's.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'after.Where(a => before.TryGetValue(a.Key, out var b) && b != a.Value)'
    Replace = 'after.Where(a => !before.TryGetValue(a.Key, out var b) || b != a.Value)'
    Tests = 'FullyQualifiedName~MigrationTests.Format_CountsTheFilesARunChanged'
}
