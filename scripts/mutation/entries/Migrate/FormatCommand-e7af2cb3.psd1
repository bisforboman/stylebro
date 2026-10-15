# Trial 23: another analyzer's failed fix isn't reported to StyleBro
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var ours = failures.Where(f => f.StartsWith("BRO", StringComparison.Ordinal)).ToList();'
    Replace = 'var ours = failures.ToList();'
    Tests = 'MigrationTests.Format_AFixThatFailed_IsReportedToWhoeverShipsIt'
}
