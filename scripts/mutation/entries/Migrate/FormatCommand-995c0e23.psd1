# Trial 27: one baseline for every project is nothing to say
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        return files.Count < 2'
    Replace = '        return files.Count < 1'
    Tests = 'MigrationTests.Baseline_GoesWhereEachProjectsBuildReadsIt'
}
