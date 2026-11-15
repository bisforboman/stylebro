# Following StyleCop 1.3.177-beta (issue 338): a chain of one bitwise operator needs no parentheses
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'
    Find = '|| (IsBitwise(a) && a.RawKind == b.RawKind);'
    Replace = ';'
    Tests = 'PrecedenceTests.BitwiseAndShiftOperations_OfOneFamilyOrUnderOtherOperators_NotReported'
}
