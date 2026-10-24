### Fixed

- **bro format/check**: in a multi-targeted repository, a project that references the library but doesn't target the
  framework being formatted (a net8.0;net462 tool next to net472 tests) is no longer forced onto that framework. The
  projects that reference it loaded again, instead of a "Not verified" for code the build compiles fine.
