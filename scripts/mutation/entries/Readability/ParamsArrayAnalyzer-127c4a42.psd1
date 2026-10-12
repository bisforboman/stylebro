# BRO1151 stylebro_params_arrays = except_object_and_jagged: arrays for params object[] stay (Sonar's S3878).
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrayAnalyzer.cs'
    Find = '&& ParamsArrays.GoesToObjectOrJagged(array, c.SemanticModel, c.CancellationToken)))'
    Replace = '&& false))'
    Tests = 'ParamsArrayTests'
}
