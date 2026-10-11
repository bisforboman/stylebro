# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'Plain(position) - joined.Where(j => j.At <= position).Sum(j => j.Breaks)'
    Replace = 'Plain(position)'
    Tests = 'ParameterLayoutTests'
}
