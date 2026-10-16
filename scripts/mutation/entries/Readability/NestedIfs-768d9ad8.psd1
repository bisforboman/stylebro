# BRO1149: an inner condition aligned under its '(' stays aligned.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '            if (current != top && !ownLine && IsAlignedUnderParenthesis(current, text))'
    Replace = '            if (false)'
    Tests = 'StyleBro.Tests.NestedIfTests.MultiLineInnerCondition_KeepsItsAlignment'
}
