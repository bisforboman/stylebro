# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '&& !SaysTheSame(lines, style))'
    Replace = ')'
    Tests = 'MigrationTests.Init_UnderTheMicrosoftStyle_SaysTheStaticsGetTheirPrefix_AndDropsLinesTheStyleRepeats'
}
