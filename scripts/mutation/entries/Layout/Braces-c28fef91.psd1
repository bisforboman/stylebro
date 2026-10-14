# BRO1514: a wrapped owner moves to its own line, its child's braces with it (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'if (wraps.FirstOrDefault(w => w.Statement == owner) is { } wrapped)'
    Replace = 'if (wraps.FirstOrDefault(w => false) is { } wrapped)'
    Tests = 'BracesTests.NestedOnOneLine_InOnePass'
}
