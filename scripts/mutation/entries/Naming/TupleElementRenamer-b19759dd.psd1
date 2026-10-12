# BRO1311: only an element other projects can see is kept for code the run doesn't load.
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'if (visible && (await'
    Replace = 'if ((await'
    Tests = 'FullyQualifiedName~KeptFindingsTests'
}
