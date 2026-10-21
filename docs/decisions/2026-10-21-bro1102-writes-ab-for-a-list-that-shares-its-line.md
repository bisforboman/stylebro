# BRO1102 writes [A][B] for a list that shares its line (2026-10-21)

## Question

Trial 40, Blazored.Toast: BRO1102 split `[Parameter, EditorRequired] public Guid ToastId { get; set; }` into
`[Parameter] [EditorRequired] public ...`, one space between the lists, as its docs said. The SDK formatter's whitespace
pass wants no space there (`dotnet format whitespace` turns `] [` into `][`, probed), so a plain `dotnet format` run
changed the file again on its second run, and `bro format` removed the space in its own later pass. How should BRO1102
write a list that shares its line with code?

## Choices

1. **`[A][B]`** (owner's pick): write what the formatter wants, no space between the lists. Converges in one plain
   `dotnet format` run.
2. Each attribute on its own line, like a list on its own line. Converges too, but moves code the author kept on one line.
3. Keep `[A] [B]` and document that the formatter removes the space on the next run.

## Decision

`[A][B]`. `CombinedAttributes.GetChange` joins the parts of a shared-line list with nothing; a list on its own line still
becomes one line per attribute.
