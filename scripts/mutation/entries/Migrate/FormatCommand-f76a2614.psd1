# Trial 18: a migrated repository (preset none) sets every rule by id
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (props is not null && Regex.IsMatch(File.ReadAllText(props), @"<StyleBroPreset>\s*none\s*<", RegexOptions.IgnoreCase))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereABulkSeverityHidesThemFromFormat'
}
