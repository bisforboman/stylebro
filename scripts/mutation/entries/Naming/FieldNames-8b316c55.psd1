# BRO1303/BRO1306: IDE1006 at warning enforces a silent naming rule.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'ide1006 is { } set and not ReportDiagnostic.Default'
    Replace = 'ide1006 is { } set and ReportDiagnostic.Default'
    Tests = 'FieldNamingTests'
}
