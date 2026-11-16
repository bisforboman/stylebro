# Following StyleCop 1.3.177-beta (issue 338): no space between BRO1401's comma and a collection expression's ]
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = ' or ''\n'' or '']'')'
    Replace = ' or ''\n'')'
    Tests = 'TrailingCommaTests.MultiLineCollectionExpressions_GetATrailingComma'
}
