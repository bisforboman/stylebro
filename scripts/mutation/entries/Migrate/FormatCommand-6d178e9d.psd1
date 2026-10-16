# Trial 24: a baseline in a folder not made yet
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Directory.CreateDirectory(Path.GetDirectoryName(file)!);'
    Replace = ''
    Tests = 'MigrationTests.Format_BaselinesKeptFindings_InABaselineOutsideTheCode'
}
