# Backlog 074 (2026-10-18): [MemberData(nameof(x))] doesn't keep x's name: Xunit.MemberData
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = 'QualifiedNameSyntax qualified => qualified.Right,'
    Replace = ''
    Tests = 'KeptFindingsTests.RepositoryNames_LeaveOutMemberDataNameofs|PascalCaseNamingTests.TheoryData_NamedByMemberDataNameof_IsRenamed'
}
