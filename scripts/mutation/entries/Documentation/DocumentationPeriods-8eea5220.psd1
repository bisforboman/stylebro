# trial 48: enum members can be left out of the period check
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = ' || (!enumMembers && Documents<EnumMemberDeclarationSyntax>(comment))'
    Replace = ''
    Tests = 'DocumentationTests.EnumMembers_AreNotCheckedForPeriods_WhenTurnedOff'
}
