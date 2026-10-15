# Trial 22: init conventions (partial parts, empty blocks, one-line properties, accessor layout, leading lambdas)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'else if (StyleBro.Analyzers.Readability.ParameterLayout.GetFirstMisplacedItem(callArguments, text) is null'
    Replace = 'else if (true'
    Tests = 'MigrationTests.Conventions_CountLeadingLambdas_EmptyBlocks_OneLineProperties_AndPartialParts'
}
