# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'BlankLineRuns.WantsBlankLineAfter(branch, branch.CloseBraceToken, item.First.GetFirstToken(), isOn, gapIsReplaced: true);'
    Replace = 'true;'
    Tests = 'ElseAfterJumpTests'
}
