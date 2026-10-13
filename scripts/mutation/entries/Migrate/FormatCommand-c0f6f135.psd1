# Whitespace exclusions only once a stylebro block sets IDE0055
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!setByStyleBro)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_SkipsTheWhitespacePass_WhereTheSettingsTurnIDE0055Off'
}
