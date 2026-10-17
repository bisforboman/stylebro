# Trial 27: an internal accessor of an assembly-only property is visible (SA1623).
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'if (DocumentationComments.GetVisibility(symbol) != Accessibility.Public)'
    Replace = 'if (false)'
    Tests = 'DocumentationTests.InternalAccessor_OfAnInternalProperty_IsNotRestricted'
}
