# IDE0049 guard: files 'bro format' lists as compiled with an older C# elsewhere (trial 35, sqlite-net)
@{
    File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'
    Find = ' && lowest < CSharpFor(core))'
    Replace = ')'
    Tests = 'MultiTargetSuppressorTests.SuppressesTypeNames_InFilesFormatListsForAnOlderBuild'
}
