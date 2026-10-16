# Trial 26: a field that keeps its name does not take its new name
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (KeepsItsName(type, f))'
    Replace = 'if (false)'
    Tests = 'FieldNamingTests.AFieldThatKeepsItsName_DoesNotTakeItsNewName'
}
