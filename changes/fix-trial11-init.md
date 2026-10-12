### Changed (stylebro-migrate only)

- **Sonar mappings** never fix what the Sonar rule itself accepts. S927 now turns BRO1313 on with
  `stylebro_match_base_parameter_names = non_generic_methods`, which skips what S927 skips (indexers, and a parameter whose
  type in the base is a type parameter the method fills in, like MediatR's `Handle(TNotification notification, ...)`).
  S100, S101, S121, S818, S1066, S1116, S1118, S1125, S1481, S1854, S1905, S2325, S3052, S3442, S3878 and S8969 are no
  longer mapped: their StyleBro or .NET rules report code those Sonar rules accept (IDE0004 removed `(int)` casts S1905
  allows, for example). The report lists them under "Not covered" with the reason.

### New

- **BRO1313** has `stylebro_match_base_parameter_names` (`all`, `non_generic_methods`).
- **`stylebro-migrate init`** follows more of your code's conventions:
  - the casing of private constants and static readonly fields (PascalCase, camelCase or `_camelCase`): a clear one is
    written as `stylebro_private_static_field_naming`, mixed turns BRO1306 off;
  - usings inside a namespace written relative to it (`using Common;`): mostly relative or mixed turns BRO1126 off;
  - file headers with empty `//` lines (a `file_header_template` that starts or ends with a line break): BRO1120 is
    turned off, so it doesn't remove what the template writes;
  - a `charset` the C# files don't follow (BOM or not): the files' charset is written for C# files, or `charset = unset`
    when they're mixed, so `format` no longer adds or strips the BOM in every file.
