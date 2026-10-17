# Trial 27: a multi-targeted build line names the framework after the project
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'tag.IndexOf("::", StringComparison.Ordinal) is var at and >= 0 ? tag.Substring(0, at) : tag'
    Replace = 'tag'
    Tests = 'MigrationTests.Verify_CountsTheFindingsOfAMultiTargetedProjectsBuild'
}
