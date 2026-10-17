# Trial 27: an evaluated StyleBroBaseline of none is off
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var none when none.Equals("none", StringComparison.OrdinalIgnoreCase) => (true, null),'
    Replace = ''
    Tests = 'MigrationTests.Baseline_GoesWhereEachProjectsBuildReadsIt'
}
