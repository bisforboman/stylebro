# BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '.OrderBy(r => r.Priority)
            .ThenByDescending'
    Replace = '.OrderBy(r => 0)
            .ThenByDescending'
    Tests = 'FieldNamingTests'
}
