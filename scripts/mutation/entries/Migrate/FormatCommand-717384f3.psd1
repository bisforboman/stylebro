# format lists shared files an older build compiles (trial 35, sqlite-net)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(g => g.Select(x => x.Version).Distinct().Count() > 1 && g.Min(x => x.Version) < new Version(12, 0))'
    Replace = '.Where(g => g.Select(x => x.Version).Distinct().Count() > 1)'
    Tests = 'MigrationTests.OldCSharpFiles_AreSharedFilesAnOlderBuildCompiles'
}
