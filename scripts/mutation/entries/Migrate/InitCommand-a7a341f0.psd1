# Trial 22 (spot 9e): only a folder that turns the Style category off
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'chain.TryGetValue("dotnet_analyzer_diagnostic.category-Style.severity", out var style) && Off(style)'
    Replace = 'chain.TryGetValue("dotnet_analyzer_diagnostic.category-Style.severity", out var style)'
    Tests = 'MigrationTests.Init_TurnsTheBuiltInRulesOff_WhereAFolderTurnsTheStyleCategoryOff'
}
