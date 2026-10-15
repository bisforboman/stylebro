# Trial 22: a type nested in the base sees its private member.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = ' || IsNestedIn(type, current)'
    Replace = ''
    Tests = 'FieldNamingTests.PrivateMemberOfABase'
}
