# Trial 22: a base's private member with the new name is no clash.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'm.DeclaredAccessibility != Accessibility.Private || IsNestedIn(type, current)'
    Replace = 'true'
    Tests = 'FieldNamingTests.PrivateMemberOfABase'
}
