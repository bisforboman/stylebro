# BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'm => m.Contains("const") && m.All(x => x is "const" or "static")'
    Replace = 'm => m.Length > 0'
    Tests = 'FieldNamingTests'
}
