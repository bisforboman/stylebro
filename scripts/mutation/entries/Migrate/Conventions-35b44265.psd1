# Trial 14 (Bogus): mixed spacing turns IDE0055 off
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '        if (mixed.Count > 0)'
    Replace = '        if (false)'
    Tests = 'MigrationTests.Spacing_MixedCodeTurnsIde0055Off'
}
