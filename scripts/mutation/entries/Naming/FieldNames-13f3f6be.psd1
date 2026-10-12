# BRO1303/BRO1306: a naming rule asking for a style StyleBro doesn't write leaves a non-private field alone.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = ' || ruled == NamingCasing.Other)'
    Replace = ')'
    Tests = 'FieldNamingTests'
}
