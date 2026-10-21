# A join that adds braces gets BRO1519 blank line from the wrap: not a second one.
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'return result == changes ? [.. changes, .. blankLines] : result;'
    Replace = 'return result == changes ? [.. changes, .. blankLines] : [.. result, .. blankLines];'
    Tests = 'FixOrderTests.ElseIf_KeepsTheBlankLine'
}
