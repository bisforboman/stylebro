# A root .editorconfig that turns the Style category off keeps the built-in rules off
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'leftOut.AddRange(styleOff.Select(id => $"dotnet_diagnostic.{id}.severity"));'
    Replace = ''
    Tests = 'MigrationTests.Init_RootStyleOff_LeavesTheBuiltInRulesOut'
}
