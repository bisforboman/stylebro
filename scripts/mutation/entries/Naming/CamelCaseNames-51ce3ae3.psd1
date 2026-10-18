# Backlog 074 (2026-10-18): [MemberData(nameof(x))] doesn't keep x's name: only MemberData
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '"MemberData" or "MemberDataAttribute" }'
    Replace = '_ }'
    Tests = 'KeptFindingsTests.RepositoryNames_LeaveOutMemberDataNameofs'
}
