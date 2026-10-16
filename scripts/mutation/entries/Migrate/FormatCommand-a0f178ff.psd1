# Trial 24 (Dawnshard): a migration designer without the attribute
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (Path.GetFileName(Path.GetDirectoryName(file)) is { } folder'
    Replace = '|| false && (Path.GetFileName(Path.GetDirectoryName(file)) is { } folder'
    Tests = 'MigrationTests.Format_AMigrationsDesigner'
}
