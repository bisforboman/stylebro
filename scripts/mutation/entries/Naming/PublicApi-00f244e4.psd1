# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/PublicApi.cs'
    Find = 'Accessibility.Public or Accessibility.Protected or'
    Replace = 'Accessibility.Public or'
    Tests = 'CamelCaseNamingTests.PublicApiParameters_AreLeftAloneByDefault'
}
