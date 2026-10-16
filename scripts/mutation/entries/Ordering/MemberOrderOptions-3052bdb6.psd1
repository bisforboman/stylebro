# Records count as classes unless record is listed (StyleCop)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'kindRanks[(int)MemberKind.Record] = kindRanks[(int)MemberKind.Class];'
    Replace = ''
    Tests = 'MemberOrderingTests.Records_RankWithClasses_ByDefault'
}
