# Trial 22: init conventions (partial parts, empty blocks, one-line properties, accessor layout, leading lambdas)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'case BlockSyntax { Statements.Count: 0 } emptyBlock when emptyBlock.Parent is not AnonymousFunctionExpressionSyntax'
    Replace = 'case BlockSyntax { Statements.Count: 0 } emptyBlock when true'
    Tests = 'MigrationTests.Conventions_CountLeadingLambdas_EmptyBlocks_OneLineProperties_AndPartialParts'
}
