# A private setter stays restricted in an internal class.
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '(restrictedSetter && !HasPrivate(setter!))'
    Replace = 'restrictedSetter'
    Tests = 'DocumentationTests.InternalAccessor_OfAnInternalProperty_IsNotRestricted'
}
