# Backlog 085: a key on two lines (a union merge) takes the larger count, not the sum
@{
    File = 'src/StyleBro.Analyzers/Baseline/Baseline.cs'
    Find = 'Math.Max(existing, count)'
    Replace = 'existing + count'
    Tests = 'BaselineTests.Render_IsSortedAndParsesBack'
}
