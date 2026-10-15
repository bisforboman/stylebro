# BRO1504's fix: the blank line below the last comment of the run
@{
    File = 'src/StyleBro.CodeFixes/Layout/BlankLineAfterCodeFixProvider.cs'
    Find = 'while (last.LineNumber + 1 < text.Lines.Count && '
    Replace = 'while (false && '
    Tests = 'BlankLineAfterTests.CommentBelowCode_LosesTheBlankLineBelowIt_WhenBRO1506IsOn'
}
