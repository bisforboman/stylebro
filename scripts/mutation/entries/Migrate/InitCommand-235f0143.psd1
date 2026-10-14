# a rule the opt-out folder sets by id stays the team's
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'return ids.Where(id => !own.ContainsKey($"dotnet_diagnostic.{id}.severity"))'
    Replace = 'return ids.Where(id => true)'
    Tests = 'MigrationTests.Init_TurnsRulesOffById_WhereAFolderTurnsEveryAnalyzerOff'
}
