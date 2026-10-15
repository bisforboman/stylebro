# init and a naming rule the code doesn't follow for non-private fields (2026-10-15)

## Question

Trial 23 (jint): the `.editorconfig` has `instance_fields_should_be_camel_case` at `suggestion` (`_camelCase`, every
instance field). By the 2026-10-13 decision an enforced naming rule decides non-private fields, so BRO1306 renamed ~545
public fields (`public int Loads` -> `_loads`), while the code has 827 PascalCase ones against 263 with `_`. Should `init`
count non-private fields against the rule?

## Choices

1. Count them like the other conventions (the fields the rule covers, named as it says or not): when the code doesn't
   clearly follow the rule (mixed, or mostly not), turn BRO1306 off with an `# init:` comment.
2. Ignore naming rules at `suggestion` for non-private fields.
3. Keep renaming to the rule.

## Decision

Choice 1 (the trial 23 task: "a naming rule at suggestion that the code contradicts must not win"; agent's choice of
how). It is init's usual pattern for a rule the code doesn't follow, needs no new key, and leaves the 2026-10-13
behavior for code that follows its rule. BRO1306 off also stops its other renames (constants, static readonly fields);
removing the line turns it back on.
