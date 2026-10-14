# Types without 'internal' turn BRO1404 off (AngleSharp).
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '                    Add(TypeAccessKey, !modifiers.Any(SyntaxKind.InternalKeyword));'
    Replace = ''
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountTypesWithoutInternal_AndCaseBlocksAtTheLabel'
}
