# the per-framework evaluations made before the restore go after it (trial 63, mono/t4)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'evaluations.Keys.Where(k => k.Contains(''|'', StringComparison.Ordinal))'
    Replace = 'evaluations.Keys.Where(k => false)'
    Tests = 'StyleBro.Tests.MigrationTests.ForgetFrameworkEvaluations'
}
