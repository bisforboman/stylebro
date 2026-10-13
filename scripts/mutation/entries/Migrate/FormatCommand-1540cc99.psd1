# Trial 14 (Bogus): only projects no configuration builds are left out
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| Regex.IsMatch(text, guid + @"\.[^\r\n]*\.Build\.0\s*=", RegexOptions.IgnoreCase))'
    Replace = ')'
    Tests = 'MigrationTests.NotBuiltFolders_AreTheProjectsNoConfigurationBuilds'
}
