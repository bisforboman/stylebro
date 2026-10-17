# Only a handler whose AppendFormatted has [CallerArgumentExpression] makes a log key
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'a.AttributeClass?.Name == "CallerArgumentExpressionAttribute"'
    Replace = 'a.AttributeClass is not null'
    Tests = 'KeptFindingsTests.ANameInALogKeyHole_IsKept'
}
