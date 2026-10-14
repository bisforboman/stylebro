# Trial 21: BRO1120 keeps the empty comments a pair of braces holds
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = ' || MarksEmptyBraces(group))'
    Replace = ')'
    Tests = 'CommentTextTests.EmptyComments_ThatAreAllABlockHolds_AreKept'
}
