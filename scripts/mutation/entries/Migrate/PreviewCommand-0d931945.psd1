# only a nuget.config the copy lacks
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '&& !File.Exists(Path.Combine(copy, f))) is { } name'
    Replace = ') is { } name'
    Tests = 'MigrationTests.PreviewCopy_SaysWhenAnIgnoredNuGetConfigStaysBehind'
}
