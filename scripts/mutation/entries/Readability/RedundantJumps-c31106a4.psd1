@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '.Any(r => r != statement)'
    Replace = '.Any(r => false)'
    Tests = 'RedundantJumpTests.LambdaWhoseEndWouldBeUnreachable'
}
