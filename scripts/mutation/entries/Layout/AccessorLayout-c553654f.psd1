# BRO1510: an attribute line doesn't make an accessor multi-line
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '(accessor.Modifiers.Count > 0 ? accessor.Modifiers[0] : accessor.Keyword).SpanStart'
    Replace = 'accessor.SpanStart'
    Tests = 'AccessorLayoutTests'
}
