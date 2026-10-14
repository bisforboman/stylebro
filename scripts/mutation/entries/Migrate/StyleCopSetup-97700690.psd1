# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'else if (IsMSBuild(file) && StyleSharpReference.IsMatch(WithoutComments(File.ReadAllText(file))))'
    Replace = 'else if (false)'
    Tests = 'MigrationTests.Init_PointsAStyleSharpRepository_'
}
