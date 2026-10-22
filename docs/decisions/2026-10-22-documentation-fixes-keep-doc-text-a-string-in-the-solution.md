# Documentation fixes keep doc text a string in the solution quotes (2026-10-22)

## Question

Trial 43, NSwag: BRO1603 turned `/// <summary>Test summary</summary>` on a test controller into `Test summary.`, and
NSwag.Generation.Tests failed (`Assert.Equal("Test summary", docTag.Description)`): NSwag (like Swashbuckle with
IncludeXmlComments) reads XML docs at run time and puts the text into the OpenAPI document. How should StyleBro's
documentation-text fixes (BRO1603 periods, BRO1604/BRO1605 property wording, BRO1606/BRO1607 standard sentences) treat
text that code may read at run time?

## Choices

1. **Keep quoted doc text** (owner's pick): like the rename guards, the fix leaves an element whose text is a string literal,
   or is in a snapshot file, in the solution; the finding stays as a kept one with the reason. Other doc fixes still apply.
2. Skip where docs are read: in projects that reference an XML-doc reader (NJsonSchema/NSwag, Swashbuckle), the
   documentation-text rules fix nothing.
3. Leave as is and document that the fixes change what XML-doc readers return.

## Decision

Keep quoted doc text: `QuotedDocumentation` (StyleBro.CodeFixes) compares the element's text, white space collapsed, with
the solution's strings and snapshot files; a text of one word, or an element holding more than text, doesn't count.
Kept findings have the reason `DocumentationTextInString` and no code action.
