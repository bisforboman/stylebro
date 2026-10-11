# BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '|| !modifiersMatch(modifiers))'
    Replace = ')'
    Tests = 'FieldNamingTests'
}
