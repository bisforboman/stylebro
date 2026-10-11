# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = ': await AddsErrorsAsync(solution, renamed, cancellationToken).ConfigureAwait(false) ? (null, KeptReason.AddsErrors)'
    Replace = ': false ? (null, KeptReason.AddsErrors)'
    Tests = 'TupleElementNamingTests'
}
