# Format host: a framework instance's load failure goes to the run of that framework
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = 'Names(d, notLoaded) || '
    Replace = ''
    Tests = 'FormatHostTests.ALoadFailure_OfAnInstance_GoesToTheRunOfItsFramework'
}
