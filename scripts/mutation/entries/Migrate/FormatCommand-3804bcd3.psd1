# Trial 59: a reflection-shaped string comes first, as the renamer picks it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var now = strings.FirstOrDefault(s => s.Kind == RepositoryNames.ReflectionKind && s.Text == name) is { Text: not null } reflection ? reflection'
    Replace = 'var now = strings.FirstOrDefault(s => s.Kind == ''x'' && s.Text == name) is { Text: not null } reflection ? reflection'
    Tests = 'MigrationTests.AKeptFindingsFirstUse_IsWhereTheFormattedTextHasIt'
}
