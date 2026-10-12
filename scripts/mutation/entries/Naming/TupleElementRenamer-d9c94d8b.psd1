# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'return await AddsErrorsAsync(solution, renamed, cancellationToken).ConfigureAwait(false) ? (null, (KeptReason.AddsErrors, null))'
    Replace = 'return false ? (null, (KeptReason.AddsErrors, null))'
    Tests = 'TupleElementNamingTests'
}
