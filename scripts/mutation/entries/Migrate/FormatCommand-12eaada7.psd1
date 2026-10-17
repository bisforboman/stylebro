# Trial 24: only a baseline turned off
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': NamedBaseline(root).Off)'
    Replace = ': NamedBaseline(root).Named)'
    Tests = 'MigrationTests.Format_BaselinesKeptFindings_InABaselineOutsideTheCode'
}
