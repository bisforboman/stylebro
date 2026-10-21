### Fixed

- **BRO1112/BRO1113** keep more of the `#region`s MarkdownSnippets includes in the docs: a `Snippet:` line in any case,
  snippets in `.mdx` pages, and a snippet used as an include (`include: Name`). Removing them made the docs step fail with
  "Missing snippet".
- **Renames** no longer keep a private field, local or parameter of a non-public member because its name appears in a
  public-API approval file (PublicApiGenerator, ApiApprover): such a file lists only the public API.
