# Projects no solution configuration builds: how to format them (AngleSharp build/).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return projects.Count == 0'
    Replace = 'return projects.Count >= 0'
    Tests = 'StyleBro.Tests.MigrationTests.NotBuiltProjects_AreNamedWithHowToFormatThem'
}
