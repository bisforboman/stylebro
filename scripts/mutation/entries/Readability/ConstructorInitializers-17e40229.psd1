# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'
    Find = '|| Line(text, close.Span.End) == Line(text, keyword.SpanStart)'
    Replace = ''
    Tests = 'ConstructorInitializerLineTests'
}
