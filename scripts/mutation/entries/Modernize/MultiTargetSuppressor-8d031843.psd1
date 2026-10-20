# IDE0049 guard: files 'bro format' lists as compiled with an older C# elsewhere (trial 35, sqlite-net)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = '.Where(f => f.Path.EndsWith(OldCSharpExtension, StringComparison.OrdinalIgnoreCase))'
    Replace = '.Where(f => false)'
    Tests = 'MultiTargetSuppressorTests.SuppressesTypeNames_InFilesFormatListsForAnOlderBuild'
}
