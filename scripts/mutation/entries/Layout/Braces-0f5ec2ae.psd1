# With CSharpier laying the code out, a statement it will split counts as multi-line.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '|| (width is { } printWidth && CSharpierBreaks('
    Replace = '|| (false && width is { } printWidth && CSharpierBreaks('
    Tests = 'BracesTests.WithCSharpier'
}
