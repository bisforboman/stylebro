# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'out var value) && value.Equals('
    Replace = 'out var value) && !value.Equals('
    Tests = 'MigrationTests.Init_UnderTheMicrosoftStyle_SaysTheStaticsGetTheirPrefix_AndDropsLinesTheStyleRepeats'
}
