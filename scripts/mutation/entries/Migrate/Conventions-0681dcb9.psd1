# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'else if (lines.Any(l => l.StartsWith(StaticCasingKey + " =", StringComparison.Ordinal)) || isSet?.Invoke(StaticCasingKey) == true)'
    Replace = 'else if (false)'
    Tests = 'MigrationTests.Init_WritesSCamelCase_ForPrefixedStatics'
}
