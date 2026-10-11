# BRO1142: a var preference that does not report is not followed
@{
    File = 'src/StyleBro.Analyzers/Readability/VarPreference.cs'
    Find = '(parts.Length > 1 && parts[1].Trim() is "suggestion" or "warning" or "error")'
    Replace = 'true'
    Tests = 'CombinedFieldsTests.Locals_KeepTheirType'
}
