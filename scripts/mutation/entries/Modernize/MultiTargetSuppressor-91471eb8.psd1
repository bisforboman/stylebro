# IDE0049 guard: files 'bro format' lists as compiled with an older C# elsewhere (trial 35, sqlite-net)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = '|| ((oldCSharp ??= ReadOldCSharp(context)).TryGetValue(NormalizePath(tree.FilePath), out var lowest) && lowest < CSharpFor(core))'
    Replace = '|| false'
    Tests = 'MultiTargetSuppressorTests.SuppressesTypeNames_InFilesFormatListsForAnOlderBuild'
}
