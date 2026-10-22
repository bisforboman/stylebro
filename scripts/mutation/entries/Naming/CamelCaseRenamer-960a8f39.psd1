# Performance audit 2026-10: the scan counter the regression test reads
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'Interlocked.Increment(ref scans.Value);'
    Replace = ''
    Tests = 'LinkedFileFixAllTests.FixAllThatSorts_ScansTheSolutionOnce'
}
