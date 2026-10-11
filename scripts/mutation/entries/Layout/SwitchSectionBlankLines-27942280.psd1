# BRO1526 (blank line between switch sections)
@{
    File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'
    Find = '|| (previous.IsKind(SyntaxKind.CloseBraceToken) && BlankLineRuns.JudgesGapAfter('
    Replace = '|| (false && BlankLineRuns.JudgesGapAfter('
    Tests = 'SwitchSectionBlankLinesTests'
}
