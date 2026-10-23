# Trial 47: a run's view formats one instance per project.
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '|| !named.Contains(path)).ToImmutableArray();'
    Replace = '|| true).ToImmutableArray();'
    Tests = 'FormatHostTests.AView_FormatsOneInstancePerProject_TheOneTheRunNames'
}
