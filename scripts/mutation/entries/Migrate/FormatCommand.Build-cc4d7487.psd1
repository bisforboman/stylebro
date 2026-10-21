# Trial 40: a project the configuration doesn't build isn't expected to compile
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '.Where(m => !Regex.IsMatch(text, Regex.Escape(m.Groups["guid"].Value) + key'
    Replace = '.Where(m => Regex.IsMatch(text, Regex.Escape(m.Groups["guid"].Value) + key'
    Tests = 'StyleBro.Tests.MigrationTests.NotBuiltIn_NamesTheProjectsAConfigurationDoesntBuild'
}
