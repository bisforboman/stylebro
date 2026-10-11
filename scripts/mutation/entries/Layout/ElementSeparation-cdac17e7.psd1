# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = '!(autoAccessorLines is not null && '
    Replace = '!(false && '
    Tests = 'ElementSeparationTests'
}
