# Following StyleCop 1.3.177-beta (issue 338): nint.Zero needs a runtime with numeric IntPtr
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = '&& compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr);'
    Replace = ';'
    Tests = 'DefaultValueConstructorTests.NativeIntegers_BecomeZeroOnNet7WithCSharp11_DefaultElsewhere'
}
