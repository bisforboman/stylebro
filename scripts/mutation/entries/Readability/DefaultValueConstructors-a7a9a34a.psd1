# Following StyleCop master: BRO1104 nint, BRO1604/BRO1605 init, BRO1611 primary constructors, BRO1606 blank summaries
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = '.Where(e => e is DefaultExpressionSyntax'
    Replace = '.Where(e => e is null'
    Tests = 'DefaultValueConstructorTests'
}
