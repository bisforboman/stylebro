# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (Winner(group[0], group[1]) != 1)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_WritesSCamelCase_ForPrefixedStatics'
}
