# BRO1603: a parenthetical closing parenthesis is prose
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '(word[word.Length - 1] == '')'' && word.IndexOf(''('') < 0)'
    Replace = 'false'
    Tests = 'DocumentationTests.TextEndingInCode'
}
