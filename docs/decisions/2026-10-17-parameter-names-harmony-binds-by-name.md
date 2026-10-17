# Parameter names Harmony binds by name (2026-10-17)

## Question

Trial 29 (Harmony): Harmony injects a patch method's parameters by their names: `__instance`, `__result`, `__state`,
`__args`, `__originalMethod`, `___field` (a private field of the patched type), and the patched method's own parameter
names. BRO1302 (SA1313) renamed `__result` to `result`, `__instance` to `instance` and `___counter` to `counter` in 9 files:
the code compiles, and the patches silently stop working. How should StyleBro keep such names?

## Choices

1. **Both**: never report a parameter whose name starts with `__`, and keep every parameter of a Harmony patch method (a
   Harmony attribute on the method or its parameters, or a patch method name in a type with a Harmony attribute).
2. Leading `__` only.
3. Harmony patch methods only.
4. Report them, but keep the name (a kept finding with a reason, baselined by `format`).

## Decision

**Choice 1: Both** (owner's choice, 2026-10-17). `BoundParameterNames.IsBound` (syntax only) runs before BRO1302,
BRO1310 and BRO1313 look at a parameter. `__` covers mods that use Harmony as a package, where the strings Harmony matches
aren't in the solution; the patch method check covers parameters named after the patched method's (`Count`). Patch
methods by name: `Prefix`, `Postfix`, `Transpiler`, `Finalizer`, `Prepare`, `Cleanup`, `TargetMethod`, `TargetMethods`;
attributes: any whose name starts with `Harmony`. StyleCop's SA1313 reports `__x`: a documented difference.
