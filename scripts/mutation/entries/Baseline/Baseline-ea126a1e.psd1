# Trial 23: a linked file named through another folder ('Tests/../Lib/A.cs') matches its entries
@{
    File = 'src/StyleBro.Analyzers/Baseline/Baseline.cs'
    Find = 'if (part == ".." && parts.Count > 1 && parts[parts.Count - 1] != "..")'
    Replace = 'if (false)'
    Tests = 'BaselineTests.ALinkedFile'
}
