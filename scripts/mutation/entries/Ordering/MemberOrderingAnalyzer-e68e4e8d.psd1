# BRO1001 leaves the members of regions BRO1112 removes to its fix (sorting within them first can end elsewhere).
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderingAnalyzer.cs'
    Find = 'if (MemberOrdering.HasRemovableRegions(container)'
    Replace = 'if (false && MemberOrdering.HasRemovableRegions(container)'
    Tests = 'FullyQualifiedName~MemberOrderingTests.Regions_ThatBRO1112Removes|FullyQualifiedName~FixOrderTests.Ordering_ACommentedMember'
}
