# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (!decided.ContainsKey(Conventions.StaticPrefixKey) && Conventions.PrefixRequiredByNamingRule(own) is { } prefix)'
    Replace = 'if (!decided.ContainsKey(Conventions.StaticPrefixKey) && Conventions.PrefixRequiredByNamingRule(new Dictionary<string, string>()) is { } prefix)'
    Tests = 'MigrationTests.Init_StaticPrefixesAndFrameworkNames_'
}
