# BRO1524 mirror: the setting decides when the other token has breaks on both sides
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = 'var side = mirror && otherBefore != otherAfter ? otherBefore : beginning;'
    Replace = 'var side = mirror ? otherBefore : beginning;'
    Tests = 'ConditionalLayoutTests'
}
