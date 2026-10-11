# Renames: a nameof that refers to the symbol keeps it
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& CamelCaseNames.IsNameofName(token))'
    Replace = '&& false)'
    Tests = 'FieldNamingTests.NameofInAnotherType|FullyQualifiedName~KeptFindingsTests.AFieldInNameofElsewhere'
}
