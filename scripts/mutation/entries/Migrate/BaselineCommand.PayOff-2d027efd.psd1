# Backlog 086: the entries come out before format runs, or the baseline hides what it should fix
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.PayOff.cs'
    Find = 'File.WriteAllText(file, new Baseline(entries.Where(e => !mine.ContainsKey(e.Key))'
    Replace = '_ = (new Baseline(entries.Where(e => !mine.ContainsKey(e.Key))'
    Tests = 'MigrationTests.PayOff_TakesOutTheFolderEntries_FormatsTheirFiles'
}
