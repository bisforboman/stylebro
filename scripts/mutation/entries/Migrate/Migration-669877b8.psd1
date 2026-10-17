# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'links.Any(l => !texts.ContainsKey(l.Project) ||'
    Replace = 'links.Any(l =>'
    Tests = 'MigrationTests.Init_SaysHowManyFieldsFollowTheNamingRule_AndCountsOnlyCompiledFiles'
}
