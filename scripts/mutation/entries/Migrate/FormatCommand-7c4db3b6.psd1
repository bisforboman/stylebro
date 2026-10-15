# Trial 22: a comment after #if tests nothing
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Regex.Matches(Regex.Replace(m.Groups["condition"].Value, "//.*$", string.Empty), @"[A-Za-z_]\w*")'
    Replace = 'Regex.Matches(m.Groups["condition"].Value, @"[A-Za-z_]\w*")'
    Tests = 'MigrationTests.Format_ComparesHowProjectsParseAFile'
}
