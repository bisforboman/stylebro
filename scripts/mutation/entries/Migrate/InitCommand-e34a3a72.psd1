# Trial 22 (spot 9e): only the built-in rules the block turns on
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.Where(p => !OffSeverity(p.Value) && !settings.ContainsKey('
    Replace = '.Where(p => true && !settings.ContainsKey('
    Tests = 'MigrationTests.Init_TurnsTheBuiltInRulesOff_WhereAFolderTurnsTheStyleCategoryOff'
}
