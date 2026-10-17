# Trial 28: a Release-only file gets a Release run
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| CompileByConfiguration(p.Value.FullPath))'
    Replace = ')'
    Tests = 'MigrationTests.Format_FindsTheConfigurationsWhoseCodeDiffers_GeneratorProjectsToo'
}
