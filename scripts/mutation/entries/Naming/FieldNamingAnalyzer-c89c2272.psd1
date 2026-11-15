# BRO1306: derived types of derived types count too
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = '                pending.Push(type);'
    Replace = ''
    Tests = 'FieldNamingTests.HidingInDerivedTypes_KeepsTheName'
}
