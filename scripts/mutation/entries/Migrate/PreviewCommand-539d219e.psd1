# preview (trial 11, Autofac): package reference lines aren't carried-over suppressions
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (projectFile && kind != "<NoWarn>")'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Preview_SettingsSummary_CountsOnlySuppressions'
}
