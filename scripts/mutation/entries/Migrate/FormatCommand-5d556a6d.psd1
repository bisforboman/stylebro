# Trial 25: one framework in TargetFrameworks is an outer evaluation without Compile items
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(frameworks.Length == 1 && EvaluatedProperty(fullPath, "TargetFrameworks").Trim().Length > 0)'
    Replace = 'false'
    Tests = 'MigrationTests.Format_FindsTheConfigurationsWhoseCodeDiffers_AndLeavesGeneratorProjectsInTheirDefault'
}
