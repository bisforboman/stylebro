# Trial 22: init conventions (partial parts, empty blocks, one-line properties, accessor layout, leading lambdas)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'blockList.Accessors.Count > 1'
    Replace = 'blockList.Accessors.Count > 0'
    Tests = 'MigrationTests.Conventions_CountLeadingLambdas_EmptyBlocks_OneLineProperties_AndPartialParts'
}
