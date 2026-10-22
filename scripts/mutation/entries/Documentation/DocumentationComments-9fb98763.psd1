# Performance: BRO1601 looks for directives only in a member that has any
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'if (!member.ContainsDirectives)'
    Replace = 'if (member.ContainsDirectives)'
    Tests = 'DocumentationTests.ConditionalDirectiveAboveTheMember_IsNotReported'
}
