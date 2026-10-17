# A child longer than the line width is multi-line.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '&& indent + child.Span.Length > width && !IsUnbreakable(child);'
    Replace = '&& indent + child.Span.Length > width + 1000 && !IsUnbreakable(child);'
    Tests = 'BracesTests.WithCSharpier'
}
