# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'out var byAttribute) ? own[ConvertedKey]'
    Replace = 'out var byAttribute) && false ? own[ConvertedKey]'
    Tests = 'MigrationTests.Init_LeavesTheLineEndingUnset_WhenGitConvertsIt'
}
