# BRO1125: a one-line body goes on the arrow's line even after a line break (Mapster: '() =>' + '{ };')
@{
    File = 'src/StyleBro.Analyzers/Readability/LambdaSyntax.cs'
    Find = 'gap.Contains("\n") && !oneLineBody ? gap'
    Replace = 'gap.Contains("\n") ? gap'
    Tests = 'LambdaSyntaxTests'
}
