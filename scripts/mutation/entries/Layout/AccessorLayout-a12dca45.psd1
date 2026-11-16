# The option set to false wins over IDE0027 being on
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '? value.Split('':'')[0].Trim().ToLowerInvariant() is'
    Replace = '? isOn(ExpressionBodiedAccessorsId) || value.Split('':'')[0].Trim().ToLowerInvariant() is'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_Reported_WhenBlockBodiesAreFine'
}
