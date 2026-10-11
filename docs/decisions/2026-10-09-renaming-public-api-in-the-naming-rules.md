# Renaming public API in the naming rules (2026-10-09)

## Question

A trial on Ocelot showed what a rename of a public name does: BRO1308 renamed
`public static readonly string X_RateLimit_Limit = nameof(X_RateLimit_Limit).Replace('_', '-');` to `XRateLimitLimit`,
which changed the HTTP header Ocelot sends. Beyond `nameof`, a public type, member, parameter (named arguments),
tuple element or namespace is a contract with code outside the solution, which the rename can't follow. Should the
naming rules (BRO1302-BRO1314) rename names other assemblies see?

```csharp
// Before
public static class RateLimitingHeaders
{
    public static readonly string X_RateLimit_Limit = nameof(X_RateLimit_Limit).Replace('_', '-');
}

// After a rename: the header is now "XRateLimitLimit"
public static class RateLimitingHeaders
{
    public static readonly string XRateLimitLimit = nameof(XRateLimitLimit).Replace('_', '-');
}
