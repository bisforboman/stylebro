# check --build's copy of HEAD gets the repository's history (fixture matrix, git-height-shallow).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '            WithHistory(untouched, repository);'
    Replace = ''
    Tests = 'StyleBro.Tests.MigrationTests.CheckBuild_BuildsTheCopyOfHead_WithTheRepositorysHistory'
}
