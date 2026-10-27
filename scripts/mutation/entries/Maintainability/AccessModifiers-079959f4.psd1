# a skipped branch counts only when it holds nothing but modifiers (a skipped declaration before one doesn't)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'
    Find = '&& IsOnlyModifiers(t.ToString())'
    Replace = ''
    Tests = 'AccessModifiersTests.ModifiersInSkippedIfBranches_StayAsTheyAre'
}
