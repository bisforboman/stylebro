# Trial 62: the preview's patch carries binary (UTF-16) files.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '["diff", "--cached", "--binary", "--ignore-submodules",'
    Replace = '["diff", "--cached", "--ignore-submodules",'
    Tests = 'StyleBro.Tests.MigrationTests.PreviewPatch_CarriesBinaryFiles'
}
