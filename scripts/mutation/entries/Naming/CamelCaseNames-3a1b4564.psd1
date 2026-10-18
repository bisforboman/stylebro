# Backlog 074 (2026-10-18): [MemberData(nameof(x))] doesn't keep x's name: only the member name argument
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = 'is AttributeArgumentSyntax { NameEquals: null, NameColon: null } argument'
    Replace = 'is AttributeArgumentSyntax argument'
    Tests = 'KeptFindingsTests.RepositoryNames_LeaveOutMemberDataNameofs'
}
