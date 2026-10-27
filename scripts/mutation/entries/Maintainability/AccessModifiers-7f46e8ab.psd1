# directives before the declaration's first token don't count (an '#if' around the declarations before it)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'
    Find = '(token != first && token.LeadingTrivia.Any(t => t.IsDirective))'
    Replace = '(token.LeadingTrivia.Any(t => t.IsDirective))'
    Tests = 'AccessModifiersTests.ModifiersInSkippedIfBranches_StayAsTheyAre'
}
