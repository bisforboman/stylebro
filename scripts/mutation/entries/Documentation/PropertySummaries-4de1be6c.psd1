# BRO1604: a restricted getter says Sets
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'var restrictedGetter = setter is not null && getAccessor is not null && IsRestricted(getAccessor);'
    Replace = 'var restrictedGetter = false;'
    Tests = 'DocumentationTests.RestrictedGetter_SaysSets'
}
