# BRO1303/BRO1306: s_camelCase is a static field style (Microsoft style G1)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '"s_camelCase" => FieldStyle.StaticCamelCase,'
    Replace = '"s_camelCase" => null,'
    Tests = 'FieldNamingTests.StaticKeys_SCamelCase_PrefixStaticFields'
}
