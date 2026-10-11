# Fix order: BRO1001 makes BRO1509's expansion and BRO1505's blank lines before sorting; a one-line type stays on one line
@{
    File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'
    Find = '.Sum(c => c.NewText!.Length - c.Span.Length);'
    Replace = '.Sum(c => 0);'
    Tests = 'FixOrderTests.MemberOrder_BlankLinesInOtherSlots'
}
