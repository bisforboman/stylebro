# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = '|| applied[applied.Count - 1].Span.End <= change.Span.Start)'
    Replace = '|| true)'
    Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAsTheOtherFixesLeaveTheLine'
}
