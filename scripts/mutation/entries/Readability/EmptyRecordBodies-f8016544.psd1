# BRO1140 (empty record body) and BRO1145 (empty class, struct or interface body)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'
    Find = 'if (frameworks.Count < 2 || '
    Replace = 'if ('
    Tests = 'EmptyTypeBodyTests'
}
