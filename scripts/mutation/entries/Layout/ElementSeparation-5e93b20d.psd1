# Trial 62: a blank line BRO1506 removes below the element's comment doesn't separate the elements.
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = '.Trim().Length == 0 && !removed.Contains(number))'
    Replace = '.Trim().Length == 0)'
    Tests = 'StyleBro.Tests.ElementSeparationTests.BlankLineBelowAComment_BRO1506Removes_DoesNotCount'
}
