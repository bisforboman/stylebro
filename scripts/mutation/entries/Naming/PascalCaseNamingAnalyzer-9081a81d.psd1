# BRO1309: a 'new' member that hides a base member keeps its name (trial 28, Jackett)
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '|| HidesBaseMember(member),'
    Replace = '|| false,'
    Tests = 'PascalCaseNamingTests.NewMembersHidingABaseMember_AreNotReported'
}
