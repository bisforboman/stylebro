# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'Edit(document, new TextChange(new TextSpan(member.SpanStart, 0), oldName + " = "));'
    Replace = ''
    Tests = 'TupleElementNamingTests'
}
