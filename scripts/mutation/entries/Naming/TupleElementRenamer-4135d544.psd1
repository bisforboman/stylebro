# BRO1311: a tuple element in a log key hole keeps its name
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'if (CamelCaseRenamer.GetLogKeyHandler(name, name.Span, model, cancellationToken) is { } handler)'
    Replace = 'if (CamelCaseRenamer.GetLogKeyHandler(name, name.Span, model, cancellationToken) is { } handler && false)'
    Tests = 'KeptFindingsTests.ATupleElementInALogKeyHole_IsKept'
}
