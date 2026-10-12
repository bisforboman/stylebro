# A PascalCase naming rule keeps BRO1306 PascalCase
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'IsPlainStyle(options, rule.Style, "pascal_case") ? null : FieldStyle.Other'
    Replace = 'FieldStyle.Other'
    Tests = 'FieldNamingTests'
}
