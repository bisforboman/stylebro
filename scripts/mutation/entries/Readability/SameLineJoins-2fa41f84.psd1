# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = 'changes.Add(ParenthesisPlacement.GetOpenChange(open, text));'
    Replace = ''
    Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAsTheOtherFixesLeaveTheLine'
}
