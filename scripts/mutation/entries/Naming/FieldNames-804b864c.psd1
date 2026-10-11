# Fields in '*NativeMethods' classes keep their names, except SA1311's lower-case static readonly ones (2026-10-11)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = ' && !HungarianNames.IsInNativeMethods(field))'
    Replace = ')'
    Tests = 'FieldNamingTests.NativeMethods_KeepFieldNames_ExceptLowerCaseStaticReadonly'
}
