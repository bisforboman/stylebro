# BRO1309: a 'new' that hides nothing doesn't keep the name
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = 'return bases.Any(b => !b.GetMembers(member.Name).IsEmpty);'
    Replace = 'return true;'
    Tests = 'PascalCaseNamingTests.ANewMemberHidingNothing_IsRenamed'
}
