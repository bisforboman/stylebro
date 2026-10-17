# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'out var styleStatic) && styleStatic == "s_camelCase")'
    Replace = 'out var styleStatic) && false)'
    Tests = 'MigrationTests.Init_UnderTheMicrosoftStyle_SaysTheStaticsGetTheirPrefix_AndDropsLinesTheStyleRepeats'
}
