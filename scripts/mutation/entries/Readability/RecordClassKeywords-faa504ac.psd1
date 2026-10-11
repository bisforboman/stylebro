# BRO1146 (record class)
@{
    File = 'src/StyleBro.Analyzers/Readability/RecordClassKeywords.cs'
    Find = 'return Trivia.IsBlank(record, span) ?'
    Replace = 'return true ?'
    Tests = 'RecordClassKeywordTests'
}
