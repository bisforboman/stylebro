# init's charset: a key the files follow stays.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (keyForm < 0 || winner == keyForm)'
    Replace = 'if (keyForm < 0)'
    Tests = 'FullyQualifiedName~MigrationTests.ContradictedCharset'
}
