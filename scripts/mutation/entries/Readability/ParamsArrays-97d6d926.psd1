# BRO1151: library calls only when a framework has .NET 9's params collections
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '? frameworks.Any(f => Modernize.MultiTargetSuppressor.Has(f, ParamsCollections)) && '
    Replace = '? '
    Tests = 'ParamsArrayTests.MultiTargeted_CallsAnotherFrameworkMayBindElsewhere_AreNotReported'
}
