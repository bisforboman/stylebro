# Format speed (2026-10-14): a kept finding is the same rename (message)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'string.Equals(Text(k.Message), Text(match.Groups["message"].Value), StringComparison.Ordinal)'
    Replace = 'true'
    Tests = 'MigrationTests.Format_ACheckWithOnlyTheKeptFindings_IsClean'
}
