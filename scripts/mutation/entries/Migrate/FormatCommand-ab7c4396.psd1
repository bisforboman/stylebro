# Off file next to a formatted one: excluded alone
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            if (onFolders.Contains(folder))'
    Replace = '            if (false)'
    Tests = 'FullyQualifiedName~Format_SkipsTheWhitespacePass_WhereTheSettingsTurnIDE0055Off'
}
