# IDE0042 only counts as a warning or an error
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = '? severity is ReportDiagnostic.Warn or ReportDiagnostic.Error'
    Replace = '? true'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
