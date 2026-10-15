# Format speed (2026-10-14): projects that parse a shared file the same share a run
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'settings(project, file) is { } b && a == b'
    Replace = 'settings(project, file) is { } b && a != b'
    Tests = 'MigrationTests.Format_ProjectsSharingAFileWithTheSameSettings_ShareARun'
}
