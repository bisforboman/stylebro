# format --verify-no-changes: only analyzer project references (source generators) have their build output copied.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'string.Equals(type.GetString(), "Analyzer", StringComparison.OrdinalIgnoreCase)'
    Replace = 'true'
    Tests = 'MigrationTests.Format_ReadsTheFrameworks'
}
