# Check coverage (2026-10-20): errors the untouched code has too are not new
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'newErrors.AddRange(group.Skip(had)'
    Replace = 'newErrors.AddRange(group'
    Tests = 'FormatCoverageTests.BuildMessages'
}
