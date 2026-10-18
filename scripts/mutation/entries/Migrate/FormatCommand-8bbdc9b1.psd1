# Trial 30 (2026-10-18): a pack listed twice is named once
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Select(p => p.GetProperty("Identity").GetString()!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()'
    Replace = '.Select(p => p.GetProperty("Identity").GetString()!).ToArray()'
    Tests = 'MigrationTests.MissingWorkloadPacks_ReadsTheSdksItems'
}
