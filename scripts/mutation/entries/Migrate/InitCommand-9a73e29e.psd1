# IDE0055 and the template's other rules go off in an opt-out folder too (keeps format's whitespace pass out)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'ids.UnionWith(Template()'
    Replace = 'ids.ExceptWith(Template()'
    Tests = 'MigrationTests.Init_TurnsRulesOffById_WhereAFolderTurnsEveryAnalyzerOff'
}
