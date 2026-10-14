# a nested folder that turns every analyzer off gets StyleBro's rules off by id (trial 20, coverlet)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if ((OptsOut(chain) ? OptOutLines('
    Replace = 'if ((false ? OptOutLines('
    Tests = 'MigrationTests.Init_TurnsRulesOffById_WhereAFolderTurnsEveryAnalyzerOff'
}
