# BRO1151: a single target sees its own overloads
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'if (frameworks.Count < 2)'
    Replace = 'if (frameworks.Count < 0)'
    Tests = 'ParamsArrayTests.MultiTargeted_CallsAnotherFrameworkMayBindElsewhere_AreNotReported'
}
