# BRO1603: a word after an operator ends code, no period.
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = ' || Operators.Contains(PreviousWord(text, start))'
    Replace = ''
    Tests = 'StyleBro.Tests.DocumentationTests.TextEndingInCodeAfterAnOperator_GetsNoPeriod'
}
