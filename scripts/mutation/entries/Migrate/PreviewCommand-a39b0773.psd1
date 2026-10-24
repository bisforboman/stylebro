# Trial 52: the preview copy uses the same extracted-package test (untracked files next to DbUp's committed .nupkg)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'Directory.Exists(directory) && StyleCopSetup.IsExtractedPackage(Directory.EnumerateFiles(directory))'
    Replace = 'Directory.Exists(directory) && Directory.EnumerateFiles(directory).Any(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))'
    Tests = 'MigrationTests.ACommittedPackageNextToTheSolution_DoesNotHideTheProjects'
}
