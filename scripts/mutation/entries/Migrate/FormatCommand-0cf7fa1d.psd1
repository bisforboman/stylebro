# format lists shared files an older build compiles (trial 35, sqlite-net)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Version.TryParse(value, out var version) ? version : new Version(int.MaxValue, 0)'
    Replace = 'Version.TryParse(value, out var version) ? version : new Version(0, 0)'
    Tests = 'MigrationTests.OldCSharpFiles_AreSharedFilesAnOlderBuildCompiles'
}
