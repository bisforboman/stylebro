# A record ranked with classes belongs to their group comment
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'keys[i + 1].KindRank == keys[i].KindRank'
    Replace = 'keys[i + 1].Kind == keys[i].Kind'
    Tests = 'MemberOrderingTests.GroupComments_CountRecordsWithClasses'
}
