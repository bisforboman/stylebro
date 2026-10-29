# Trial 62: BRO1505's fix removes the blank line below the comment that BRO1506 would (else BRO1506 takes the comment for standalone).
@{
    File = 'src/StyleBro.CodeFixes/Layout/ElementSeparationCodeFixProvider.cs'
    Find = '                changes.AddRange(ElementSeparation.GetCommentBlankLines(current, text, commentBlankLines).Select(l => new TextChange(l.SpanIncludingLineBreak, string.Empty)));'
    Replace = ''
    Tests = 'StyleBro.Tests.ElementSeparationTests.BlankLineBelowAComment_BRO1506Removes_DoesNotCount'
}
