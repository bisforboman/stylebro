# Backlog 085: the line goes on a line of its own after a file without a final newline
@{
    File = 'src/StyleBro.Migrate/GitAttributes.cs'
    Find = 'text.EndsWith(''\n'') ? text : text + "\n"'
    Replace = 'text'
    Tests = 'GitAttributesTests.KeepsTheFile_AndItsLineEndings'
}
