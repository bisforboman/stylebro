# BRO1303/BRO1311: the registration's rename check is reused by Fix All (fix-loop measurement, 2026-10)
@{
    File = 'src/StyleBro.CodeFixes/Naming/RenameChecks.cs'
    Find = 'var checks = Cache.GetValue(solution, _ => new ConcurrentDictionary<string, Lazy<Task<object>>>(StringComparer.Ordinal));'
    Replace = 'var checks = new ConcurrentDictionary<string, Lazy<Task<object>>>(StringComparer.Ordinal);'
    Tests = 'RenameChecksTests'
}
