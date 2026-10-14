# format isn't clean while a nested .editorconfig's bulk severity hides rules the build reports (trial 19)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Concat(InitCommand.NestedHiddenFromFormat(Path.GetFullPath(root), settings, withBlocks: true).Values.SelectMany(l => l))'
    Replace = ''
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereANested'
}
