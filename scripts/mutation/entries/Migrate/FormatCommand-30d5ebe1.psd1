# Trial 27: a package's files in any packages folder are not outside the repository
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!f.StartsWith(nuget, StringComparison.OrdinalIgnoreCase) && !IsPackageFile(f))'
    Replace = '!f.StartsWith(nuget, StringComparison.OrdinalIgnoreCase))'
    Tests = 'MigrationTests.Format_PlansNoRunsForAPackagesFiles'
}
