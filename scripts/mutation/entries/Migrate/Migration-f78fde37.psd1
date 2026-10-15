# Trial 22 (spot d2): a .globalconfig counts only with is_global = true
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (lines.Any(l => Regex.IsMatch(l, @"^is_global\s*=\s*true\b", RegexOptions.IgnoreCase)))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Init_ReadsTheRootGlobalConfig'
}
