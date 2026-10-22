# Performance audit 2026-10: a Fix All that sorts reads the names of the solution it started from (one scan, not one per file)
@{
    File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'
    Find = 'cancellationToken, namesFrom).ConfigureAwait(false) is null)'
    Replace = 'cancellationToken).ConfigureAwait(false) is null)'
    Tests = 'LinkedFileFixAllTests.FixAllThatSorts_ScansTheSolutionOnce'
}
