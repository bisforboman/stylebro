# Trial 21: BRO1120 keeps the empty comments a pair of braces holds
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = '.All(IsEmpty);'
    Replace = '.Any(IsEmpty);'
    Tests = 'CommentTextTests.EmptyComments_ThatAreAllABlockHolds_AreKept'
}
