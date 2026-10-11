# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (GetBranchToWrap(node, text, options, isOn) is { } branch && Braces.GetChanges(new[] { branch }, text, options) is null)'
    Replace = 'if (false)'
    Tests = 'ElseAfterJumpTests'
}
