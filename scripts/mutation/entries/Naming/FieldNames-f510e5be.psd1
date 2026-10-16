# BRO1306: constants stay PascalCase with s_camelCase
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'style == FieldStyle.StaticCamelCase ? null : style'
    Replace = 'style'
    Tests = 'FieldNamingTests.StaticKeys_SCamelCase_PrefixStaticFields'
}
