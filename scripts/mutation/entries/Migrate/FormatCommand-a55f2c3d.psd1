# Trial 25: missing compiled files: IDE0005 is excluded
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'result.AddRange(["--exclude-diagnostics", UnusedUsings]);'
    Replace = '_ = result;'
    Tests = 'MigrationTests.Format_SaysWhichCompiledFilesAreMissing'
}
