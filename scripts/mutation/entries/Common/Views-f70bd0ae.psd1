# Trial 47: a run's references go to the instances it names.
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '&& instance != referenced.Id)'
    Replace = '&& instance != referenced.Id && false)'
    Tests = 'FormatHostTests.AView_ReferencesTheInstancesTheRunNames'
}
