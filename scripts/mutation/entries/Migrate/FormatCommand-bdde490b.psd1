# Trial 23: a .vb file a fix changed is put back
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Where(f => !File.Exists(f.Key) || !File.ReadAllBytes(f.Key).AsSpan().SequenceEqual(f.Value))'
    Replace = 'Where(f => !File.Exists(f.Key))'
    Tests = 'MigrationTests.Format_LeavesVisualBasicAlone'
}
