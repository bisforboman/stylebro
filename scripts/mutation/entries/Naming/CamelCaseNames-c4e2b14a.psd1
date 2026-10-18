# Backlog 074 (2026-10-18): [MemberData(nameof(x))] doesn't keep x's name: only the first argument
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '&& list.Arguments.IndexOf(argument) == 0'
    Replace = ''
    Tests = 'KeptFindingsTests.RepositoryNames_LeaveOutMemberDataNameofs'
}
