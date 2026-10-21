# Trial 39: only a change of root namespace counts.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'UsingName(list[i - 1]).Split(''.'')[0] != UsingName(list[i]).Split(''.'')[0]'
    Replace = 'true'
    Tests = 'MigrationTests.SeparatesRootNamespaces_IsTrueFalseOrMixed'
}
