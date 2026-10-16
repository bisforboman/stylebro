# Trial 24 (Refit): an excluded empty element at the end needs no period
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'if (BlockElements.Contains(emptyName) || excluded.Contains(emptyName))'
    Replace = 'if (BlockElements.Contains(emptyName))'
    Tests = 'DocumentationTests.ExcludedEmptyElements_AtTheEnd_AreNotCheckedForPeriods'
}
