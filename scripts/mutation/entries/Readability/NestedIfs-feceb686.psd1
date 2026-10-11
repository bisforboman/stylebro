# Only conditional directives in the if keep it (regions are removed by BRO1113)
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '            || (outer.ContainsDirectives && HasConditionalDirective(outer.DescendantTrivia()))'
    Replace = '            || outer.ContainsDirectives'
    Tests = 'NestedIfTests'
}
