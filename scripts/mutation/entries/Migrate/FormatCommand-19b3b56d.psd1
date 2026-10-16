# Trial 25: projects whose code tests a configuration symbol get configuration runs
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(p => p.Value.Compile.Any(f => TestedSymbols(f).Any(s => !IsFrameworkSymbol(s))))'
    Replace = '.Where(p => false)'
    Tests = 'MigrationTests.Format_FindsTheConfigurationsWhoseCodeDiffers_AndLeavesGeneratorProjectsInTheirDefault'
}
