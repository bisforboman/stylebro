# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(d => !d.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))'
    Replace = ''
    Tests = 'MigrationTests.EveryStyleBroRule_NamesTheStyleCopRulesItReplaces'
}
