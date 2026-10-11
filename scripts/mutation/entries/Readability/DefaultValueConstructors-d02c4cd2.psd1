# Following StyleCop master: BRO1104 nint, BRO1604/BRO1605 init, BRO1611 primary constructors, BRO1606 blank summaries
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = '>= LanguageVersion.CSharp7_1'
    Replace = '>= LanguageVersion.CSharp1'
    Tests = 'DefaultValueConstructorTests'
}
