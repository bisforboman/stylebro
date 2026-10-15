# BRO1151 in a multi-targeted project: another framework may bind the elements elsewhere (trial 23)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '            || MayBindElsewhere(method, options, cancellationToken)'
    Replace = ''
    Tests = 'ParamsArrayTests.MultiTargeted_CallsAnotherFrameworkMayBindElsewhere_AreNotReported'
}
