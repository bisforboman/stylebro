# BRO1603 drops trailing spaces at the line end
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'end == text.Length || text[end] is ''\r'' or ''\n'' || '
    Replace = ''
    Tests = 'FullyQualifiedName~PeriodAtTheEndOfALine_TakesThePlaceOfTrailingSpaces'
}
