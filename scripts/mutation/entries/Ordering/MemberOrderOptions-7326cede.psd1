# BRO1001: stylebro_member_order_scope = fields_first (Microsoft style G3)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = '            return FieldsFirst;'
    Replace = '            return Default;'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
