# Only under obj/.
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& path.Replace(''\\'', ''/'').IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0;'
    Replace = ';'
    Tests = 'KeptFindingsTests.BuildMetadataUnderObj_HoldsNoNames'
}
