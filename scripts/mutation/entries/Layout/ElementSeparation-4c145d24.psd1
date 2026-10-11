# A documented property is set apart (BRO1505), BRO1601 adds the blank line below (Ocelot)
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = '|| HasDocumentation(property) || HasDocumentation(next),'
    Replace = ','
    Tests = 'ElementSeparationTests.DocumentedProperties_AreSetApart'
}
