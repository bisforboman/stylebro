# BRO1519/BRO1504 leave switch-section gaps to BRO1526 when it omits the blank line
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = '        mode != Mode.Include'
    Replace = '        false'
    Tests = 'SwitchSectionBlankLinesTests|FullyQualifiedName~BlankLineAfterTests.CommentAboveASwitchSection'
}
