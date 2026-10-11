# BRO1603: a number-only text gets no period
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = ' || IsNumberOnly(element) ? null'
    Replace = ' ? null'
    Tests = 'DocumentationTests.TextEndingInCode'
}
