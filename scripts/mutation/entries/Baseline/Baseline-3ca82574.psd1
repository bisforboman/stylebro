# Trial 24: a baseline outside the code covers files through ../ paths
@{
    File = 'src/StyleBro.Analyzers/Baseline/Baseline.cs'
    Find = 'return common == 0'
    Replace = 'return true'
    Tests = 'BaselineTests.ABaselineInAnotherFolder'
}
