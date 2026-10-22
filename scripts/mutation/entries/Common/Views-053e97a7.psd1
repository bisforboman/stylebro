# Format host: a load failure of an instance goes to the run that asks for it
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '(d.Kind != WorkspaceDiagnosticKind.Failure && Names(d, own))'
    Replace = 'Names(d, own)'
    Tests = 'FormatHostTests.ALoadFailure_OfAnInstance_GoesToTheRunThatAsksForIt'
}
