# A field is not renamed to its type's name (CS0542, sourcegit)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (newName == type.Name'
    Replace = 'if (false'
    Tests = 'FieldNamingTests'
}
