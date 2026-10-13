# Copies: only a repository whose MSBuild files name OutputItemType has its projects evaluated for generators.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Any(f => File.ReadAllText(f).Contains("OutputItemType", StringComparison.OrdinalIgnoreCase))'
    Replace = '.Any(f => false)'
    Tests = 'MigrationTests.Copies_GetTheBuildOutputOfInSolutionGenerators'
}
