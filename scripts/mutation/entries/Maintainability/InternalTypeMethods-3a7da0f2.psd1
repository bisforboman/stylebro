# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'
    Find = 'node.ContainsDirectives && node.DescendantTrivia().Any(IsConditional)'
    Replace = 'node.ContainsDirectives'
    Tests = 'InternalTypeMethodTests'
}
