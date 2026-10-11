# BRO1526 (blank line between switch sections)
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = ' && node.Sections[i - 1].Statements.Last() is not BlockSyntax'
    Replace = ''
    Tests = 'SwitchSectionBlankLinesTests'
}
