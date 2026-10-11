# BRO1526 (blank line between switch sections)
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = 'if (!previous.TrailingTrivia.LastOrDefault().IsKind(SyntaxKind.EndOfLineTrivia)'
    Replace = 'if (false'
    Tests = 'SwitchSectionBlankLinesTests'
}
