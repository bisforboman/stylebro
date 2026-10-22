# Trial 42: only the held-back gap is one the builds can check
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '|| !line.EndsWith(suffix, StringComparison.Ordinal))'
    Replace = ')'
    Tests = 'MigrationTests.HeldBackGapIds_'
}
