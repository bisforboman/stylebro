# BRO1140 (empty record body) and BRO1145 (empty class, struct or interface body)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'
    Find = '|| type.Members.Count > 0'
    Replace = ''
    Tests = 'EmptyRecordBodyTests'
}
