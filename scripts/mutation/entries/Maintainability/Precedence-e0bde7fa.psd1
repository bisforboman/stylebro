# Following StyleCop 1.3.177-beta (issue 338): >>> is a shift like << and >>
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'
    Find = 'or SyntaxKind.GreaterThanGreaterThanGreaterThanToken;'
    Replace = ';'
    Tests = 'PrecedenceTests.BitwiseAndShiftOperations_OfOneFamilyOrUnderOtherOperators_NotReported'
}
