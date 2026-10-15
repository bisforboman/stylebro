# Records: SA1214 isn't checked by StyleCop 1.2 (trial 23)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'readonlyRank: !inRecord && options.ReadonlyFirst'
    Replace = 'readonlyRank: options.ReadonlyFirst'
    Tests = 'MemberOrderingTests.Record_StaticConstantAndReadonlyNotChecked_LikeStyleCop'
}
