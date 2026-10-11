# Only conditional directives after the if keep it (an #endregion made BRO1149 need a second run)
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '            || HasConditionalDirective(outer.GetLastToken().GetNextToken().LeadingTrivia)'
    Replace = '            || outer.GetLastToken().GetNextToken().LeadingTrivia.Any(t => t.IsDirective)'
    Tests = 'NestedIfTests'
}
