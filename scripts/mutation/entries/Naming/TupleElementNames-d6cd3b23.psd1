# BRO1311: a literal converted to a nullable tuple type follows its names
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = '            type = nullable.TypeArguments[0];'
    Replace = ''
    Tests = 'TupleElementNamingTests.NamesInTupleLiterals_ThatFollowADeclaredTupleType'
}
