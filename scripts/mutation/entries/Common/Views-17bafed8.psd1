# Format host: a framework instance's load failure goes to the views that have that instance
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = 'Names(d, ownFailed) || '
    Replace = ''
    Tests = 'FormatHostTests.ALoadFailure_OfAnInstance_GoesToTheRunThatHasIt'
}
