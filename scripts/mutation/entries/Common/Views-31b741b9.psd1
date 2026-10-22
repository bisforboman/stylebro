# Format host: a framework instance's load failure doesn't go to the views with the project's other instances
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '!(d.Kind == WorkspaceDiagnosticKind.Failure && Names(d, failed)) && '
    Replace = ''
    Tests = 'FormatHostTests.ALoadFailure_OfAnInstance_GoesToTheRunThatHasIt'
}
