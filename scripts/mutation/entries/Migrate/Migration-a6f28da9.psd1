# Trial 52: the converted-minority note only when IDE0055 formats
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (formattingOn && minorityNote is not null)'
    Replace = 'if (minorityNote is not null)'
    Tests = 'MigrationTests.Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts'
}
