# An accessor without a body leaves the list alone
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = 'accessors.Any(a => a.Body is null && a.ExpressionBody is null)'
    Replace = 'accessors.Any(a => false)'
    Tests = 'AccessorLayoutTests.NotReported'
}
