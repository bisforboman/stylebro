# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = ': compiled.TryGetValue(owner, out var items) && items is not null && !items.Contains(full);'
    Replace = ': compiled.TryGetValue(owner, out var items) && items is not null;'
    Tests = 'MigrationTests.Init_CountsOnlyCompiledFiles'
}
