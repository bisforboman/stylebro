# A header longer than the line width is multi-line.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'before.Span.End - ownerLine.Start > width)'
    Replace = 'before.Span.End - ownerLine.Start > width + 1000)'
    Tests = 'BracesTests.WithCSharpier'
}
