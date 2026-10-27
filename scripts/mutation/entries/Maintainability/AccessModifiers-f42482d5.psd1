# BRO1404 adds no modifier where an '#if' branch skips one (DapperAOT, trial 57)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'
    Find = 'if (HasConditionalModifiers(node.GetFirstToken(), location))'
    Replace = 'if (false && HasConditionalModifiers(node.GetFirstToken(), location))'
    Tests = 'AccessModifiersTests.ModifiersInSkippedIfBranches_StayAsTheyAre'
}
