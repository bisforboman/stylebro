# Trial 15: a comment over several lines goes along.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'inComment = !line.Contains("<!--", StringComparison.Ordinal);'
    Replace = 'inComment = false;'
    Tests = 'FullyQualifiedName~MigrationTests.ThePackageVersion_DoesNotSplitACommentFromItsItem'
}
