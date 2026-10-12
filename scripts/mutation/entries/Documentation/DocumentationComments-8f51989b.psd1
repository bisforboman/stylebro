# BRO1602: a '///' after code on its line is fixed too (Mapster: reported, never fixed)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'number == first ? comment.FullSpan.Start - line.Start : '
    Replace = ''
    Tests = 'DocumentationTests'
}
