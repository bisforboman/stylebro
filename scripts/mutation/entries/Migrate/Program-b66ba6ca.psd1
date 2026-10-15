# Without StyleCop settings the migration calls nothing leftovers
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'Console.WriteLine(evidence.Count == 0'
    Replace = 'Console.WriteLine(evidence.Count < 0'
    Tests = 'MigrationTests.Migrate_NoStyleCopAtAll_DoesntCallAnythingLeftovers'
}
