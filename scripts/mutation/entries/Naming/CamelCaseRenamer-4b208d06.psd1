# Trial 38: internal members aren't in a public-API listing
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)'
    Replace = 'is Accessibility.Private'
    Tests = 'KeptFindingsTests.IsInPublicApi'
}
