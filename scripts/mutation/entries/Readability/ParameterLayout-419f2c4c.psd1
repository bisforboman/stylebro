# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'joined is null || !joinsEmptyLists() ? Plain'
    Replace = 'joined is null ? Plain'
    Tests = 'ParameterLayoutTests'
}
