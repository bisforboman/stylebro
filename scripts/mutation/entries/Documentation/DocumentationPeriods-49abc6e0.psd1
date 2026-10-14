# Trial 21: SA1629 skips events and finalizers
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '|| IsUnchecked(comment)'
    Replace = ''
    Tests = 'DocumentationTests.EventsAndFinalizers_AreNotChecked'
}
