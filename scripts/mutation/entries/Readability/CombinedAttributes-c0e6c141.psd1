# Trial 40: a shared-line split writes [A][B], as the whitespace pass does
@{
    File = 'src/StyleBro.Analyzers/Readability/CombinedAttributes.cs'
    Find = 'indentation : string.Empty;'
    Replace = 'indentation : " ";'
    Tests = 'StyleBro.Tests.CombinedAttributesTests'
}
