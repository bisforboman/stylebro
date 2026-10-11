# Trial 8, tool (2026-10-11): the per-framework runs include files projects link from elsewhere
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return folders.Concat(outside)'
    Replace = 'return folders'
    Tests = 'MigrationTests.Format_IncludesTheFilesProjectsLink'
}
