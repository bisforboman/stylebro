# StyleBro's version goes into the analyzers' ItemGroup (AngleSharp).
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'versions = versions.Where(m => GroupOf(m) == GroupOf(analyzer)).ToList();'
    Replace = ''
    Tests = 'StyleBro.Tests.MigrationTests.MSBuildEdits_FollowTheFilesIndentation_AndTheAnalyzersItemGroup'
}
