# BRO1513's fix adds BRO1505's blank line above a directive too (trial 60, EasyCaching).
@{
    File = 'src/StyleBro.CodeFixes/Layout/DocumentationBlankLinesCodeFixProvider.cs'
    Find = 'separations ??= GetSeparations(document, root, text, cancellationToken);'
    Replace = 'separations ??= [];'
    Tests = 'DocumentationBlankLinesTests.BlankLineBeforeDocumentation_BelowARegionRightAfterAMember_GetsBRO1505sToo'
}
