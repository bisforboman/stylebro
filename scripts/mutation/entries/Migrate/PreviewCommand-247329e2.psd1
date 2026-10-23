# trial 48: the preview's copy leaves untracked package files behind
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '            .Where(e => !untracked.Contains(e) || !InPackage(source, e, packageFolders)))'
    Replace = '            .Where(e => !untracked.Contains(e) || true))'
    Tests = 'MigrationTests.Copy_LeavesUntrackedFilesOfExtractedPackagesBehind'
}
