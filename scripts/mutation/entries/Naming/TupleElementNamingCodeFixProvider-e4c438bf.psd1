# Kept findings (2026-10-10): a rename the fix keeps on purpose offers no code action
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementNamingCodeFixProvider.cs'
    Find = '&& await TupleElementRenamer.GetKeptReasonAsync(context.Document.Project.Solution, rename.OldName, rename.NewName, context.CancellationToken).ConfigureAwait(false) is null)'
    Replace = ')'
    Tests = 'TupleElementNamingTests'
}
