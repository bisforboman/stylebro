# Records: SA1204 isn't checked by StyleCop 1.2 (trial 23, vs-streamjsonrpc)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'staticRank: !inRecord && options.StaticFirst'
    Replace = 'staticRank: options.StaticFirst'
    Tests = 'MemberOrderingTests.Record_StaticConstantAndReadonlyNotChecked_LikeStyleCop'
}
