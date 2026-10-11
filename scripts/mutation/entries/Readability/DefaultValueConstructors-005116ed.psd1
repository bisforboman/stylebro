# Following StyleCop master: BRO1104 nint, BRO1604/BRO1605 init, BRO1611 primary constructors, BRO1606 blank summaries
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = '&& typeText is not ("nint" or "nuint"))'
    Replace = ')'
    Tests = 'DefaultValueConstructorTests'
}
