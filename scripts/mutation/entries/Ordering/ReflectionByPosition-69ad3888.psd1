# BRO1001: First() reads by position
@{
    File = 'src/StyleBro.Analyzers/Ordering/ReflectionByPosition.cs'
    Find = '"First" or "FirstOrDefault"'
    Replace = '"NotFirst" or "FirstOrDefault"'
    Tests = 'MemberOrderingTests.MembersOtherThanTheOnesReadByPosition|FullyQualifiedName~KeptFindingsTests.RepositoryNames_ListTheTypesReadByPosition'
}
