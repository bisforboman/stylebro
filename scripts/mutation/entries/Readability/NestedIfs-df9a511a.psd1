# A merged condition longer than the line is broken.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '+ flat.Length + 1 <= width'
    Replace = '+ flat.Length + 1 <= width + 1000'
    Tests = 'FullyQualifiedName~NestedIfTests.WithCSharpier'
}
