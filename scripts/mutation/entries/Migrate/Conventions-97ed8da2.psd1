# init's BRO1120 for file headers: a severity the repository sets stays.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (own.ContainsKey($"dotnet_diagnostic.{rule}.severity"))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.EmptyHeaderLines'
}
