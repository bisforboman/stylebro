# StyleCop spacing rules that are off: the code decides the SDK spacing options (2026-10-15)

## Question

kafkaflow (StyleCop.Analyzers 1.1.118) turns SA1000, SA1003 and SA1009 off in its ruleset; its code has a few `new ()`
and `catch(`. The migration turned IDE0055 on (other spacing rules are on), and IDE0055, which enforces every SDK spacing
option, rewrote them (trial 22). What should the migration do for StyleCop spacing rules that are off?

## Choices

1. **Map each disabled spacing rule to the SDK formatting option that expresses the repository's code**, judged like
   `init` does (a clear majority: at least 3 places, all of them below 10, 75 % from 10 on). Where no SDK option expresses
   it, keep IDE0055 and name the difference in the report.
2. Turn IDE0055 off whenever a spacing rule is off.
3. Keep IDE0055 as is, without a word.

## Decision

**Choice 1** (owner's choice 2026-10-15). The aspects and their options, probed with `dotnet format whitespace` on SDK
10.0.401:

| StyleCop rule off | What is counted | SDK option written when the code clearly does the other thing |
|---|---|---|
| SA1000 | `if(x)` (also `foreach`, `while`, `lock`, `using`, `switch`, `catch`) | `csharp_space_after_keywords_in_control_flow_statements = false` |
| SA1000 | target-typed `new ()` | none: `csharp_space_between_method_call_name_and_opening_parenthesis` also spaces every call (`F (x)`, `new T ()`), which SA1008 reports; a note |
| SA1003 | `a+b` | `csharp_space_around_binary_operators = none`; mixed: `ignore` |
| SA1003 and SA1009 | `(int) x` | `csharp_space_after_cast = true` |
| SA1008 and SA1009 | `( x )` in control-flow statements, expressions, casts; `F( x )`, `void F( int x )` | `csharp_space_between_parentheses`, `csharp_space_between_method_call_parameter_list_parentheses`, `csharp_space_between_method_declaration_parameter_list_parentheses` |

Not expressible either: `return(x)` (IDE0055 always writes `return (x)`). kafkaflow's code follows the SDK's spacing in all
but 3 places (1 `catch(`, 2 `new ()`), so IDE0055 stays on there and the report names those changes.
