# BRO1303/BRO1306/BRO1307 (fields)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = '&& !(rule == FieldRule.Prefix && FieldNames.IsPrefixRequiredByNamingRule(field.Name.Substring(0, 2), options))'
    Replace = ''
    Tests = 'FieldNamingTests'
}
