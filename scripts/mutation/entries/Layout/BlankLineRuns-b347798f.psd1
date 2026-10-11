# BRO1526 (blank line between switch sections)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = '&& OpeningLine(brace, text) != text.Lines.GetLineFromPosition(brace.SpanStart).LineNumber;'
    Replace = ';'
    Tests = 'SwitchSectionBlankLinesTests'
}
