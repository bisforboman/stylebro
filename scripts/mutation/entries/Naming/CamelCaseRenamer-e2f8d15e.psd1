# Trial 21: an outer member is hidden by the renamed field
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '!IsHiddenByTheRename(s, field, newName) && '
    Replace = ''
    Tests = 'FieldNamingTests.NestedField_IsRenamedPastAnOuterFieldWithTheNewName'
}
