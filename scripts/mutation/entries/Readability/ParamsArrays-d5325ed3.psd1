# BRO1151: an overload behind #if in the method's type
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'if (directive.IsKind(SyntaxKind.IfDirectiveTrivia))'
    Replace = 'if (false)'
    Tests = 'ParamsArrayTests.MultiTargeted_CallsAnotherFrameworkMayBindElsewhere_AreNotReported'
}
