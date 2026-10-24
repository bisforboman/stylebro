# Trial 52: no note when no file has the other ending
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'return others == 0
            ? null'
    Replace = 'return false
            ? null'
    Tests = 'MigrationTests.Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts'
}
