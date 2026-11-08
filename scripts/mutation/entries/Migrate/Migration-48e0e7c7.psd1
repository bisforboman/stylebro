# Issue #339: SA1412 and SX1412 both on carry neither charset over
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.IsOn("SA1412") && setup.IsOn("SX1412"))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.BothByteOrderMarkRules_LeaveTheCharsetAlone'
}
