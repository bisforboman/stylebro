# a folder below one that got lines takes them into account
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var found = i < parts.Length && result.TryGetValue(folder, out var above)'
    Replace = 'var found = false && result.TryGetValue(folder, out var above)'
    Tests = 'MigrationTests.NestedHiddenFromFormat_AFolderBelowAnOptOutFolder'
}
