# Backlog 074 (2026-10-18): [MemberData(nameof(x))] doesn't keep x's name
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '            && !IsMemberDataName(nameof);'
    Replace = '            ;'
    Tests = 'PascalCaseNamingTests.TheoryData_NamedByMemberDataNameof_IsRenamed|FieldNamingTests.TheoryDataField_NamedByMemberDataNameof_IsRenamed'
}
