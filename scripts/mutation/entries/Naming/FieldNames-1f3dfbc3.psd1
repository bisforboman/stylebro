# A private camelCase field next to its property is still renamed.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (char.IsUpper(newName[0]) && type'
    Replace = 'if (type'
    Tests = 'FieldNamingTests.PascalName_DifferingOnlyInCaseFromAMember_IsKept'
}
