# BRO1603: no period after code
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = ' || EndsInCode(untrimmed) ? null'
    Replace = ' ? null'
    Tests = 'DocumentationTests.TextEndingInCode'
}
