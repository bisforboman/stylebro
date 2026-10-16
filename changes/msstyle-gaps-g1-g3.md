### New

- `s_camelCase` for `stylebro_private_mutable_static_field_naming` and `stylebro_private_static_field_naming`: static
  fields get the .NET runtime's `s_` prefix (`count`, `_count` and `m_count` become `s_count`, `s_` is kept; constants
  stay PascalCase).
- `stylebro_internal_field_naming = like_private` names internal and private protected fields like private ones
  (`internal int _count;`, `internal static int s_count;`) instead of StyleCop's PascalCase.
- `stylebro_member_order_scope = fields_first` makes BRO1001 check only that fields come first in a type, keeping the
  order of everything else (the .NET runtime's rule 15).
