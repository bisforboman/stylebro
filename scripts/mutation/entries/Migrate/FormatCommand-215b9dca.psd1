# Trial 27: a package's files plan no runs
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'File.Exists(path) && !IsPackageFile(path) ? TestedIn'
    Replace = 'File.Exists(path) ? TestedIn'
    Tests = 'MigrationTests.Format_PlansNoRunsForAPackagesFiles'
}
