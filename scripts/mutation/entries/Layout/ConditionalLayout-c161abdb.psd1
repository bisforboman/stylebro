# BRO1524 mirrors only with BRO1520 off
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = 'var side = mirror && otherBefore != otherAfter ? otherBefore : beginning;'
    Replace = 'var side = otherBefore != otherAfter ? otherBefore : beginning;'
    Tests = 'ConditionalLayoutTests'
}
