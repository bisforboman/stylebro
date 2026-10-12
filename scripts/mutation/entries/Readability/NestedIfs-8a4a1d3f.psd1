# A merge under CSharpier lays the condition out as CSharpier would.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'CSharpierCondition(ifs, text, top, layout.Width, layout.Unit) is { } laidOut)'
    Replace = 'CSharpierCondition(ifs, text, top, layout.Width, layout.Unit) is { } laidOut && false)'
    Tests = 'FullyQualifiedName~NestedIfTests.WithCSharpier'
}
