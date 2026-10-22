# Trial 43: the .globalconfig files of the folders above count (the SDK reads them), without scanning below those folders.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '            .Concat((above ?? []).Select(f => Path.Combine(f, ".globalconfig")).Where(File.Exists));'
    Replace = '            ;'
    Tests = 'MigrationTests.HiddenLine_CountsAGlobalConfigAboveTheFolder'
}
