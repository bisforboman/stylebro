# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = '&& !(isOn(DiagnosticIds.ElseAfterJump) && e.Parent is IfStatementSyntax owner && ElseAfterJump.IsCandidate(owner, text, options, isOn))'
    Replace = '&& true'
    Tests = 'ElseIfTests'
}
