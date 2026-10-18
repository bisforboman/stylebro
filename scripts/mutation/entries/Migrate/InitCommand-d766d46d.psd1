# init (trial 32, RazorLight): the ending sections are written
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '            scopes.AddRange(literalSections);'
    Replace = ''
    Tests = 'MigrationTests.LineEndings_AFileWhoseStringLiteralSpansLines'
}
