# C# projects reaching a VB project transitively are left out by the old engine (fixture matrix, visual-basic).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return memo[path] = references.Any(r => r.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) || Reaches(Path.GetFullPath(r)));'
    Replace = 'return memo[path] = references.Any(r => r.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase));'
    Tests = 'StyleBro.Tests.MigrationTests.ReachingVisualBasic_FollowsCSharpReferences'
}
