# Format speed (2026-10-14): the folder with or without a trailing separator
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'LastKept[Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))] = kept;'
    Replace = 'LastKept[Path.GetFullPath(root)] = kept;'
    Tests = 'MigrationTests.Format_ACheckWithOnlyTheKeptFindings_IsClean'
}
