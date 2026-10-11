# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'if (InferredNameStart(name) is { } start)'
    Replace = 'if (InferredNameStart(name) is { } start && false)'
    Tests = 'TupleElementNamingTests'
}
