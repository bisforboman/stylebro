# Trial 22: the renames are applied to the solution with its unresolved analyzer references.
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'return await ApplyAsync(original, changes, cancellationToken)'
    Replace = 'return await ApplyAsync(solution, changes, cancellationToken)'
    Tests = 'KeptFindingsTests.UnresolvedAnalyzerReference'
}
