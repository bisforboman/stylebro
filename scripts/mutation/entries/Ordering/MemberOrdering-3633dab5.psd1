# BRO1001: a blank line BRO1519 put below a moved member's '}' goes with it.
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '                layout = SyntaxFactory.TriviaList();'
    Replace = ''
    Tests = 'MemberOrderingTests.Bro1519sBlankLine_IsNotLeftBetweenTwoFields'
}
