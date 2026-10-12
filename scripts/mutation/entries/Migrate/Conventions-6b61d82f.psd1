# Trial 12 (2026-10-12): init follows insert_final_newline, column alignment, BRO1527, BRO1113, one-line jumps, mixed code under an unenforced key
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& (body is BlockSyntax || Line(ifStatement.IfKeyword.SpanStart) == Line(jump.Span.End)))'
    Replace = ')'
    Tests = 'MigrationTests.Conventions_CountOneLineJumps'
}
