# Trial 8, tool (2026-10-11): StyleCop 1.0.x lacks SA1135, SA1136, SA1137, SA1139, SA1314, SA1413
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (version.StartsWith("1.0.", StringComparison.Ordinal))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.StyleCop10_'
}
