# init/migrate (trial 32): only files of the other ending get a section
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '|| LineEnding(text) is not (0 or 1) || LineEnding(text) == target)'
    Replace = '|| LineEnding(text) is not (0 or 1))'
    Tests = 'MigrationTests.LineEndings_AFileWhoseStringLiteralSpansLines'
}
