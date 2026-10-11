# Fix order: BRO1001 makes BRO1509's expansion and BRO1505's blank lines before sorting; a one-line type stays on one line
@{
    File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'
    Find = 'var violations = IsOn(DiagnosticIds.ElementsSeparatedByBlankLine)'
    Replace = 'var violations = true'
    Tests = 'MemberOrderingTests'
}
