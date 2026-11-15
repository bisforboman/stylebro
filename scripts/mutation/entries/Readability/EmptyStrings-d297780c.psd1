# Following StyleCop 1.3.177-beta (issue 338): BRO1106 reports an empty raw string
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = ' or SyntaxKind.MultiLineRawStringLiteralToken'
    Replace = ''
    Tests = 'EmptyStringTests.EmptyInterpolatedAndRawStrings_BecomeStringEmpty'
}
