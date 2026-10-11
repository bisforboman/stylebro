# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'if (after.All(f => before.TryGetValue(f.Child.SpanStart, out var id) && id == f.Id))'
    Replace = 'if (true)'
    Tests = 'ElseIfTests'
}
