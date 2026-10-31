# init counts neighbouring one-line members for stylebro_allow_adjacent_single_line_members (trial 63, mono/t4)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '!adjacent && !StyleBro.Analyzers.Layout.ElementSeparation.NeedsBlankLine(type.Members[i - 1], type.Members[i], text, allowAdjacentSingleLine: true) ? AdjacentMembersKey : SeparationKey'
    Replace = 'SeparationKey'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountNeighbouringOneLineMembersApartFromBRO1505'
}
