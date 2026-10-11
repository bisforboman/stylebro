# BRO1142: var only where it infers the same type
@{
    File = 'src/StyleBro.Analyzers/Readability/VarPreference.cs'
    Find = '&& SymbolEqualityComparer.Default.Equals(inferred, declared);'
    Replace = ';'
    Tests = 'CombinedFieldsTests.Locals_WhereVar'
}
