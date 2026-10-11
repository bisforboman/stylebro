# BRO1150: List/array Exists and Find where Sonar S6605/S6602 are on
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = '|| !(isArray ? symbol.ContainingType.SpecialType == SpecialType.System_Array : IsList(symbol.ContainingType)))'
    Replace = ')'
    Tests = 'WhereBeforeTerminalTests.CollectionMethodsThatWouldNotBind'
}
