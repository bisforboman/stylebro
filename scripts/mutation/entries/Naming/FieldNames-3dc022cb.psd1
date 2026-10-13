# BRO1303/BRO1306: without IDE1006 set, a silent naming rule is not enforced.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = ': severity is "suggestion" or "warning" or "error");'
    Replace = ': true);'
    Tests = 'FieldNamingTests'
}
