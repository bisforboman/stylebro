# init sets StyleBro's rules by id where a nested .editorconfig's bulk severity hides them from 'dotnet format' (trial 19)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'foreach (var (source, hidden) in NestedHiddenFromFormat(root, blockSettings, withBlocks: false, written, styleSettings))'
    Replace = 'foreach (var (source, hidden) in new SortedDictionary<string, List<string>>())'
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereANested'
}
