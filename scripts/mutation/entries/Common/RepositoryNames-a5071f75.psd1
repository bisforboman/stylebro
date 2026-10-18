# Backlog 074 (2026-10-18): [MemberData(nameof(x))] doesn't keep x's name: in format's names file too
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = '&& !Naming.CamelCaseNames.IsMemberDataName(invocation))'
    Replace = ')'
    Tests = 'KeptFindingsTests.RepositoryNames_LeaveOutMemberDataNameofs'
}
