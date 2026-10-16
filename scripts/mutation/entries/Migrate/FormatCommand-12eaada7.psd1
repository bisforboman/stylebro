# Trial 24: only a baseline turned off
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'NamedBaseline(root) is (true, null)'
    Replace = 'NamedBaseline(root) is (true, _)'
    Tests = 'MigrationTests.Format_BaselinesKeptFindings_InABaselineOutsideTheCode'
}
