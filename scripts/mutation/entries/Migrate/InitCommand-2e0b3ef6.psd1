# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '== StyleBro.Analyzers.Naming.FieldStyle.UnderscoreCamelCase ? 1 : 0]'
    Replace = '== StyleBro.Analyzers.Naming.FieldStyle.UnderscoreCamelCase ? 0 : 1]'
    Tests = 'MigrationTests.Init_SaysHowManyFieldsFollowTheNamingRule_AndCountsOnlyCompiledFiles'
}
