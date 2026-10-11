# BRO1001: order the runtime sees
@{
    File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'
    Find = 'var serialized = HasSerializerAttribute(type.AttributeLists) || members.Any(m => HasSerializerAttribute(m.AttributeLists));'
    Replace = 'var serialized = false;'
    Tests = 'MemberOrderingTests'
}
