# Kept findings say where the string is (trial of 0.5.0-alpha.1, 2026-10-10)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'KeptFindings.Record(diagnostic, kept, where);'
    Replace = 'KeptFindings.Record(diagnostic, kept);'
    Tests = 'KeptFindingsTests'
}
