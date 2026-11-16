# BRO1306: a protected readonly field keeps its leading underscore in the _camelCase style ('_logger')
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = ' || (ruled is null && isProtectedReadOnly && style.Private != FieldStyle.CamelCase)'
    Replace = ''
    Tests = 'FieldNamingTests.ProtectedFields_KeepTheirUnderscore_InTheUnderscoreStyle'
}
