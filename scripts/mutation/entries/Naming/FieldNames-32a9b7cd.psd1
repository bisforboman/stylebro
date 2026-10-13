# BRO1303/BRO1306: IDE1006 off (none) means no naming rule is enforced.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '? set is ReportDiagnostic.Info or ReportDiagnostic.Warn or ReportDiagnostic.Error'
    Replace = '? true'
    Tests = 'FieldNamingTests'
}
