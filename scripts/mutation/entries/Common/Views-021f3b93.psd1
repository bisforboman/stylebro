# Format host: a framework instance's load failure doesn't go to the runs whose framework loaded
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '!(d.Kind == WorkspaceDiagnosticKind.Failure && Names(d, failed)) && '
    Replace = ''
    Tests = 'FormatHostTests.ALoadFailure_OfAnInstance_GoesToTheRunOfItsFramework'
}
