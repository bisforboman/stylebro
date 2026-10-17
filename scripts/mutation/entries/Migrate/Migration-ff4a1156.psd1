# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'Unreadable[Path.GetFullPath(file)] = 0;'
    Replace = '_ = file;'
    Tests = 'MigrationTests.Init_SaysWhichFilesItCouldntRead'
}
