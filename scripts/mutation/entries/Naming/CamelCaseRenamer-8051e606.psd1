# Trial 27: obj/ assembly attributes are no evidence.
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'documents.AddRange(project.Documents.Where(d => !IsBuildMetadataFile(d.FilePath)));'
    Replace = 'documents.AddRange(project.Documents);'
    Tests = 'KeptFindingsTests.BuildMetadataUnderObj_HoldsNoNames'
}
