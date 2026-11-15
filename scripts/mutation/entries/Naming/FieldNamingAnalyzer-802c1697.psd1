# BRO1306: a derived type's member with the new name would hide the renamed field
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = '            && !IsHiddenInDerivedType(field, newName, derived)'
    Replace = ''
    Tests = 'FieldNamingTests.HidingInDerivedTypes_KeepsTheName'
}
