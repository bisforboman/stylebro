# Trial 22 (spot 9e): only the built-in rules the block turns on
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var ids = last.Where(p => !Off(p.Value))'
    Replace = 'var ids = last.Where(p => true)'
    Tests = 'MigrationTests.Init_TurnsTheBuiltInRulesOff_WhereAFolderTurnsTheStyleCategoryOff'
}
