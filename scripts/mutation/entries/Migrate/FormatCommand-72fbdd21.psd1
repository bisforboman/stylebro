# Trial 8, tool (2026-10-11): --files finds the project that links a file
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (target is null && LinkingProject('
    Replace = 'if (false && LinkingProject('
    Tests = 'MigrationTests.Files_ALinkedFile'
}
