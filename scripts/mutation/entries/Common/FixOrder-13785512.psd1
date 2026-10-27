# A fix step keeps the suppressors of its ids (fixture matrix, solution-in-subfolder: the baseline's suppressor).
@{
    File = 'src/StyleBro.FormatHost/FixOrder.cs'
    Find = '        [.. analyzers.Where(a => a is DiagnosticSuppressor suppressor
            ? suppressor.SupportedSuppressions.Any(s => ids.Contains(s.SuppressedDiagnosticId))'
    Replace = '        [.. analyzers.Where(a => a is DiagnosticSuppressor suppressor
            ? false'
    Tests = 'StyleBro.Tests.FormatHostFixTests.AFixStepsAnalysis_KeepsTheSuppressorsOfItsIds'
}
