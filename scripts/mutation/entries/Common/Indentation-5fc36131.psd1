# Trial 47: new lines below a tab-indented line get spaces with indent_style = space.
@{
    File = 'src/StyleBro.Analyzers/Indentation.cs'
    Find = 'if (unit.IndexOf(''\t'') >= 0 || whitespace.IndexOf(''\t'') < 0)'
    Replace = 'if (true)'
    Tests = 'ParameterLayoutTests.TabIndentedLine_WithSpaceIndentation_GetsSpaces|ParenthesisPlacementTests.OwnLine_TabIndentedLine_WithSpaceIndentation_GetsSpaces|ConstraintPlacementTests.TabIndentedLine_WithSpaceIndentation_GetsSpaces'
}
