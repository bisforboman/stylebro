# Off file next to a formatted one: excluded alone
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' && !onFolders.Contains(folder) ? folder'
    Replace = ' ? folder'
    Tests = 'MigrationTests.Format_SkipsTheWhitespacePass_WhereTheSettingsTurnIDE0055Off'
}
