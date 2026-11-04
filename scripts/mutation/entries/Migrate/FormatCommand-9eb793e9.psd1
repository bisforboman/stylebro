# The analyzer pass runs apart with the CodeStyle package (079).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'codeStyle && passes.Contains(string.Empty) ?'
    Replace = 'false && passes.Contains(string.Empty) ?'
    Tests = 'FormatLineEndingsTests.SeparatePasses'
}
