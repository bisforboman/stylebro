# BRO1310: s_ stays in front of the stripped name
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'FieldStyle.StaticCamelCase => "s_", '
    Replace = ''
    Tests = 'HungarianAbstractTests.StaticPrefix_StaysInFrontOfTheStrippedName'
}
