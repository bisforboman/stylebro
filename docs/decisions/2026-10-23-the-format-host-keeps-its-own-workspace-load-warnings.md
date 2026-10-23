# The format host keeps its own workspace load warnings (2026-10-23)

## Question

On Newtonsoft.Json the two engines of `bro format` give the same files, exit codes and reports, but one note differs.
The default engine's net20 run warns "Found project reference without a matching metadata reference:
Newtonsoft.Json.csproj", an artefact of forcing `TargetFramework=net20` through an environment variable. The in-process
host loads every framework at once, resolves that reference and has no such warning, so its "had warnings loading the
workspace" note names the net46 run instead of net20. Should the host mimic the old warning?

## Choices

1. **Keep the host's warnings and document the difference**: the host's load is the more correct one; the parity results
   and docs list this note as an expected difference.
2. Mimic the old warning, so the output matches byte for byte: extra code that reproduces a load artefact.

## Decision

**Choice 1** (owner's choice 2026-10-23). error-or's net462 run (its tests target net462, the library doesn't) has the same
note with the default engine and none with the host.
