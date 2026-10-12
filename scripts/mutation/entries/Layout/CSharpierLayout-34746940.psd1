# Only with stylebro_layout_by = csharpier.
@{
    File = 'src/StyleBro.Analyzers/Layout/CSharpierLayout.cs'
    Find = 'if (!options.TryGetValue(Key, out var by)'
    Replace = 'if (options.TryGetValue(Key, out var by)'
    Tests = 'FullyQualifiedName~BracesTests.WithoutCSharpier|FullyQualifiedName~BracesTests.WithCSharpier'
}
