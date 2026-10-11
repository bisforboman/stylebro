# BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '.ThenByDescending(r => r.Modifiers)'
    Replace = ''
    Tests = 'FieldNamingTests'
}
