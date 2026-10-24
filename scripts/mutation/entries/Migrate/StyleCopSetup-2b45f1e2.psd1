# Trial 52: a folder with a project, solution or Directory.Build.* file is not an extracted package (DbUp src/dbup.1.2.3.nupkg)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '&& !names.Any(n => Regex.IsMatch('
    Replace = '&& !names.Any(n => false && Regex.IsMatch('
    Tests = 'MigrationTests.ACommittedPackageNextToTheSolution_DoesNotHideTheProjects'
}
