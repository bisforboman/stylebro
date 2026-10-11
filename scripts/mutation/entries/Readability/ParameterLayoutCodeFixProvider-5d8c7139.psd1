# With BRO1108 on, BRO1107's fix puts every item on its own line (moving the first item alone could leave the next item's continuation lines behind).
@{
    File = 'src/StyleBro.CodeFixes/Readability/ParameterLayoutCodeFixProvider.cs'
    Find = 'All: allForFirst ||'
    Replace = 'All: false ||'
    Tests = 'FullyQualifiedName~FixOrderTests.ParameterLayout_AnItemOnTheLineWhereAMovedLambdaEnds'
}
