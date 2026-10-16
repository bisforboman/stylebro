# BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '!IsPrivateLike(field, style) || !IsSourceField(field)'
    Replace = '!IsSourceField(field)'
    Tests = 'FieldNamingTests'
}
