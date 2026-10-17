# Trial 28: verify counts doc indentation only without documentation generation
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!generateDocumentationFile.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);'
    Replace = 'true;'
    Tests = 'MigrationTests.Verify_CountsDocCommentIndentation_OnlyWhereTheBuildReportsIt'
}
