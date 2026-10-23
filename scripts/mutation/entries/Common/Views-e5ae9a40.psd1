# Trial 47: a run's references go to the instances it names.
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = 'if (ofTheRun && referenced.FilePath is { } path && named.TryGetValue(path, out var instance) && instance != referenced.Id)'
    Replace = 'if (false)'
    Tests = 'FormatHostTests.AView_ReferencesTheInstancesTheRunNames'
}
