# format isn't clean while a nested .editorconfig's bulk severity hides rules the build reports (trial 19)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Concat(InitCommand.NestedHiddenFromFormat(top, settings, withBlocks: true, written).Values.SelectMany(l => l))'
    Replace = ''
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereANested'
}
