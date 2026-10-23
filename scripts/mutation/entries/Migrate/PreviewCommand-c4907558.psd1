# trial 48: tracked files in a package folder are still copied
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '.Where(e => !untracked.Contains(e) || !InPackage'
    Replace = '.Where(e => !InPackage'
    Tests = 'MigrationTests.Copy_LeavesUntrackedFilesOfExtractedPackagesBehind'
}
