# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'if (InferredNameStart(name, model, cancellationToken) is { } start)'
    Replace = 'if (InferredNameStart(name, model, cancellationToken) is { } start && false)'
    Tests = 'TupleElementNamingTests'
}
