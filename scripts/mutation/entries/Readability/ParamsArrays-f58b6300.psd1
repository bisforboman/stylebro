# BRO1151 except_object_and_jagged: jagged params arrays too.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '|| element is IArrayTypeSymbol);'
    Replace = ');'
    Tests = 'ParamsArrayTests'
}
