# Format speed (2026-10-14): the owner's settings, not the project's twice
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'owner.Equals(project, StringComparison.OrdinalIgnoreCase) || (settings?.Invoke(owner, file) is { } a'
    Replace = 'owner.Equals(project, StringComparison.OrdinalIgnoreCase) || (settings?.Invoke(project, file) is { } a'
    Tests = 'MigrationTests.Format_ProjectsSharingAFileWithTheSameSettings_ShareARun'
}
