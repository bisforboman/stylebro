# A private setter stays restricted in an internal class.
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'restrictedSetter = restrictedSetter && HasPrivate(setter!);'
    Replace = 'restrictedSetter = false;'
    Tests = 'DocumentationTests.InternalAccessor_OfAnInternalProperty_IsNotRestricted'
}
