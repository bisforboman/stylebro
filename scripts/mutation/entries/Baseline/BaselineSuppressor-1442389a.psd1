# Backlog 073 (2026-10-18): xUnit fixes in format: the baseline covers them (owner)
@{
    File = 'src/StyleBro.Analyzers/Baseline/BaselineSuppressor.cs'
    Find = '.Concat(XunitIds).Distinct'
    Replace = '.Distinct'
    Tests = 'MigrationTests.Baseline_CoversTheXunitRulesFormatFixes'
}
