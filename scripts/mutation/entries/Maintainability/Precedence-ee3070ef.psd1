# Following StyleCop 1.3.177-beta (issue 338): &, | and ^ are each a family of their own
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'
    Find = '(IsBitwise(a) && a.RawKind == b.RawKind)'
    Replace = '(IsBitwise(a) && IsBitwise(b))'
    Tests = 'PrecedenceTests.BitwiseAndUnsignedShiftOperands_GetParentheses'
}
