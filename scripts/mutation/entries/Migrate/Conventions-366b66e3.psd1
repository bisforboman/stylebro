# A 'case' block at the label's indentation counts as csharp_indent_case_contents_when_block = false (AngleSharp).
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Add(IndentCaseBlockKey, text.Lines.GetLinePosition(caseBlock.SpanStart).Character <= '
    Replace = 'Add(IndentCaseBlockKey, text.Lines.GetLinePosition(caseBlock.SpanStart).Character > '
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountTypesWithoutInternal_AndCaseBlocksAtTheLabel'
}
