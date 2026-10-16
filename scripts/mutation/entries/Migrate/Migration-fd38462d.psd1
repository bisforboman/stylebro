# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (File.Exists(props) || FormatCommand.RepositoryRoot(start) is not { } top)'
    Replace = 'if (FormatCommand.RepositoryRoot(start) is not { } top)'
    Tests = 'MigrationTests.ANewPropsFile_SaysItHidesOneAboveTheRepository'
}
