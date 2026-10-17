# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'Unreadable.TryRemove(Path.GetFullPath(file), out _);'
    Replace = '_ = file;'
    Tests = 'MigrationTests.Init_SaysWhichFilesItCouldntRead'
}
