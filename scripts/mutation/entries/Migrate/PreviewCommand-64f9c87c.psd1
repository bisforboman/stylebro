# Trial 59: check --build's HEAD copy gets the repository's tags (MinVer)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (Git(source, "for-each-ref", "--format=%(objectname) %(refname)", "refs/tags") is (0, var tags) && tags.Trim().Length > 0)'
    Replace = 'if (false && Git(source, "for-each-ref", "--format=%(objectname) %(refname)", "refs/tags") is (0, var tags) && tags.Trim().Length > 0)'
    Tests = 'MigrationTests.TheHeadCopy_HasTheRepositorysTags'
}
