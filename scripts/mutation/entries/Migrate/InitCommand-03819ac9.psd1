# a 'root = true' folder that turns every analyzer off gets only its rules-off block (trial 20)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (folder.Length > 0 && !migrated && OptsOut(ownSettings))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_TurnsRulesOffById_WhereAFolderTurnsEveryAnalyzerOff'
}
