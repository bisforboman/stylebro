# BRO1526 omit_after_block: only after a section ending in a block
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = '(mode == Mode.Omit || '
    Replace = '(true || '
    Tests = 'BlankLineAfterTests.CommentAboveASwitchSection_IsReported_WhenOmitAfterBlock'
}
