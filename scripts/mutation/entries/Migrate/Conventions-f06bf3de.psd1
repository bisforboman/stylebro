# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'counts[StaticReadOnlyPrefixKey][variable.Identifier.ValueText is [''s'' or ''t'', ''_'', _, ..] ? 1 : 0]++;'
    Replace = 'counts[StaticReadOnlyPrefixKey][1]++;'
    Tests = 'MigrationTests.Init_WritesSCamelCase_ForPrefixedStatics'
}
