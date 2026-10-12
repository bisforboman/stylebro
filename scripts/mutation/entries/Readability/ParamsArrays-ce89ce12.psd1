# BRO1151 except_object_and_jagged: params Array[] too.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'SpecialType.System_Object or SpecialType.System_Array ||'
    Replace = 'SpecialType.System_Object ||'
    Tests = 'ParamsArrayTests'
}
