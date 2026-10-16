# BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '"camelCase" => FieldStyle.CamelCase,
            "_camelCase" => FieldStyle.UnderscoreCamelCase,
            "s_camelCase" => FieldStyle.StaticCamelCase,
            _ => null,'
    Replace = '"camelCase" => FieldStyle.CamelCase,
            "_camelCase" => FieldStyle.UnderscoreCamelCase,
            "s_camelCase" => FieldStyle.StaticCamelCase,
            _ => FieldStyle.CamelCase,'
    Tests = 'FieldNamingTests'
}
