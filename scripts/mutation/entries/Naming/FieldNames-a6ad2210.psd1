# Trial 27: no PascalCase name that differs only in case from another member.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (char.IsUpper(newName[0]) && type.MemberNames.Any('
    Replace = 'if (false && type.MemberNames.Any('
    Tests = 'FieldNamingTests.PascalName_DifferingOnlyInCaseFromAMember_IsKept'
}
