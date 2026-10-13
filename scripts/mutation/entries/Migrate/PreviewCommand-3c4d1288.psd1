# Trial 15 (Fluid): the PackageVersion goes above the comment of the next item.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (!inComment && !line.EndsWith("-->", StringComparison.Ordinal))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.ThePackageVersion_DoesNotSplitACommentFromItsItem'
}
