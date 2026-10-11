# BRO1303/BRO1306/BRO1307 (fields)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'GetNamingRules(options, m => m.Length == 0)'
    Replace = 'GetNamingRules(options, m => true)'
    Tests = 'FieldNamingTests'
}
