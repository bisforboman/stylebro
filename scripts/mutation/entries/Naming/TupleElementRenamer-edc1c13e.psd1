# BRO1311: a rename that lets IDE0042 report is dropped
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = ': await Deconstruction.AddsReportsAsync(solution, renamed, cancellationToken).ConfigureAwait(false) ? (null, (KeptReason.AddsDeconstruction, null))'
    Replace = ''
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
