# BRO1147 allow_null_forgiving_on_unannotated: a var local counts only when its initializer is unannotated.
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '&& IsUnannotated(initializer, model, cancellationToken),'
    Replace = '&& true,'
    Tests = 'NullForgivingTests'
}
