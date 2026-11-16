# Following StyleCop 1.3.177-beta (issue 338): an empty interpolated string that isn't a string stays
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = '&& model.GetTypeInfo(interpolated, cancellationToken).ConvertedType?.SpecialType is SpecialType.System_String or SpecialType.System_Object,'
    Replace = ','
    Tests = 'EmptyStringTests.EmptyInterpolatedStrings_AsFormattableOrHandler_Stay'
}
