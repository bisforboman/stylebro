# Trial 9, tool (2026-10-12): projects that compile the same files run apart
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| Same(owner, p, f))));'
    Replace = '|| true)));'
    Tests = 'MigrationTests.Format_ProjectsThatCompileTheSameFiles'
}
