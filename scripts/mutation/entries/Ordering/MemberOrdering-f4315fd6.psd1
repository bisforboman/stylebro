# Records: SA1203 isn't checked by StyleCop 1.2 (trial 23)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'constantRank: !inRecord && options.ConstantsFirst'
    Replace = 'constantRank: options.ConstantsFirst'
    Tests = 'MemberOrderingTests.Record_StaticConstantAndReadonlyNotChecked_LikeStyleCop'
}
