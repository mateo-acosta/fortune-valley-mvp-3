# Fortune Valley — Proof of Concept (Claude Rules)

## 0. Primary Objective (Non-Negotiable)
This project is a Unity proof-of-concept to test ONE hypothesis:

"A game that requires financial knowledge to progress causes students to learn."

All decisions must optimize for:
- Conceptual clarity
- Learning signal strength
- Development speed
- Low complexity

This is NOT a production build.

---

## 1. Game Summary
Genre: 2.5D idle-clicker / light city-builder
Perspective: Isometric / 2.5D
Player Role: Restaurant chain owner expanding into a small city
Core Tension: Spend money now vs invest to grow money over time

Progression Model:
- The city is a fixed grid of 7 interactive building lots plus ambient scenery (roads, park, background buildings).
- The player starts with one Tier 2 restaurant on one lot (their established starter location). A rival AI starts with one Tier 2 restaurant on another lot. The remaining lots start empty.
- Empty lots are purchase signals only. A "For Sale" sign marks them. There is no elaborate empty-lot visual.
- When the player purchases an empty lot, it spawns a **Tier 1 dilapidated restaurant** on that lot (a rundown version of the same silhouette as T2). The player then upgrades it through the three tiers: **Tier 1 = dilapidated, Tier 2 = finished/normal, Tier 3 = thriving/standout**.
- The narrative arc is "you bought a rundown property and restored it into a neighborhood favorite, then into the best restaurant in town."
- The rival expands in parallel using the same three-tier silhouette with a corporate franchise skin (different materials and props, same silhouette per tier).

Soft Pressure (NOT a hard win/lose):
- Lots are finite. If the player does not buy a lot, the rival can, and it is locked to the rival from then on. This creates opportunity cost without a hard ending.
- Monthly obligations (loan payments, credit card statements, insurance premiums) create cash-flow pressure.

Hard Lose Condition:
- Bankruptcy is the only hard lose state. Five consecutive insolvent months triggers a reset with a permanent bankruptcy flag, per the financial systems design.

No Hard Win Condition:
- The game does not end in a win. The player keeps expanding indefinitely. Success is measured by sustained chain growth, tier progression, and financial health relative to the rival, not by a terminal screen.

Explicitly Out of Scope for POC:
- No decay during play. Buildings do not degrade over time. A Tier 2 restaurant does not revert to Tier 1. The player must actively upgrade, but never has to "maintain" against decay.
- No intermediate sub-tiers between the three main tiers. Three tiers only: dilapidated, normal, thriving.
- Investing has no world-visible effect. Portfolio value changes numbers in the UI only. A skyline-linked investing visual is a post-POC stretch goal.
- The 7 `CityLotDefinition` assets (Lot_Bakery, Lot_Bistro, Lot_Corner, Lot_Diner, Lot_Hotel, Lot_Tower, Lot_Cafe) are district flavor labels only. All use the same hero restaurant mesh with the same three tiers. They do NOT imply different building silhouettes per cuisine type.

---

## 2. Core Learning Outcomes (Must Be Explicit)
The game must surface these concepts clearly and repeatedly:

- Opportunity cost
- Compound interest
- Time value of money
- Risk vs reward

The player should be able to verbally explain:
- WHY an investment decision helped or hurt them
- WHY waiting sometimes beats immediate spending

---

## 3. Core Systems (POC Scope Only)

### 3.1 Restaurant Income System
- Generates currency every X seconds
- Predictable, low-risk baseline income
- Serves as the "safe but slow" option

### 3.2 Investment System (Learning Core)
- Supports multiple financial instruments
- Explicit compounding logic
- Player can see:
  - Principal
  - Gains / losses
  - Time invested
  - Rate of return
- Outcomes must be explainable in plain language

### 3.3 Rival Expansion System
- AI competitor buying city lots and upgrading restaurants in parallel with the player
- Provides soft time pressure via lot scarcity: lots the rival buys are permanently lost to the player
- Rival uses the same restaurant base model with a corporate franchise skin (material and prop variants)
- Rival is a pacing mechanism, not a lose condition. The rival cannot end the game.
- Forces meaningful trade-offs about which lots to prioritize and when to borrow vs save

---

## 4. Architectural Principles

### 4.0 Objectivity Standard

Every rule passes this test: compliance is determined by searching for a specific string, counting a specific thing, or checking a lookup table. No classification or intent judgment is required. Where judgment cannot be removed, the rule reads "stop and ask the user" -- Claude never makes that call.

---

### 4.1 Architecture Principles Table

`Classification` column: **BLOCKING** = must resolve before writing any new code. **ADVISORY** = must resolve before merging, does not block current file.

| Principle | Detectable Violation | Required Action | Classification | Source |
|-----------|---------------------|-----------------|----------------|--------|
| Layer dependency direction | A `using` statement in a file points to a namespace in a forbidden direction. Forbidden pairs (check this exact lookup table): Domain imports anything; Core imports FortuneValley.Managers or FortuneValley.UI; Managers imports FortuneValley.UI. | Delete the forbidden `using`. Replace the dependency with `GameEvents.Raise*()`. If it cannot be solved without the forbidden import, stop and ask the user. Do not write any code until resolved. | BLOCKING | Clean Architecture (Robert C. Martin) |
| One type per file | Count `class `, `struct `, `enum `, `interface ` keyword occurrences in the file. If the count is greater than 1, violation. | Stop. Create a separate `.cs` file for each additional type before continuing. No exceptions. | BLOCKING | C# conventions + Unity serialization |
| No duplicate types | Search the project for the exact type name about to be created. If it exists in any file, violation. | Stop. Do not create the type. Either use the existing one or delete the existing one first in the same change. Both versions must never exist simultaneously. | BLOCKING | DRY |
| asmdef per layer | Count `.asmdef` files present in `Domain/`, root `Scripts/` (Core), `Managers/`, `UI/`. Required: exactly 1 per folder, 4 total. Any missing is a violation. | Stop. Create the missing `.asmdef` before writing any code in that layer. | BLOCKING | Unity assembly definition best practice |
| Naming conventions | Search the file for: (1) any private field not beginning with `_`; (2) any class or enum name not beginning with a capital letter; (3) any interface name not beginning with `I`; (4) any event field not beginning with `On`. Each match is a violation. | Rename to match the required pattern. No exceptions. | ADVISORY | Microsoft C# conventions |
| No numeric literals in gameplay code | Search the file for bare numeric literals (`0.5f`, `100`, `0.25f`, etc.) that are not: (1) `0`, `1`, or `-1` used in arithmetic identity; (2) inside a `Debug.Log` or `string.Format` call; (3) inside a test file. Each remaining match is a violation. | Stop. Move the value to a named `[SerializeField] private` field or a ScriptableObject config field. | ADVISORY | Unity best practice |
| Event-driven cross-system communication | Search any file for a method call (contains `(`) on a `[SerializeField]` reference whose declared type is defined in a different layer assembly. A property getter (no parameters, used for reading) is not a violation. A method call with parameters or whose return value is used in a computation is a violation. | Stop. Replace the direct method call with a `GameEvents.Raise*()` call. The receiving system subscribes and acts. | BLOCKING | Observer pattern |
| No singletons | Search the file for: `static` combined with `Instance` or `_instance` as a field or property name, or `FindFirstObjectByType` used to assign a static field. Each match is a violation unless the class is: (1) a platform SDK wrapper (Analytics, Ads, Crash reporting) containing zero game logic, OR (2) the user has given explicit written approval for this specific class name in the current session. | Stop. Ask the user. Do not proceed without explicit approval. | BLOCKING | Gang of Four |
| No FindFirstObjectByType | Search for the strings `FindFirstObjectByType` or `FindObjectOfType` in any non-Editor `.cs` file. Each match is a violation. Exception: `InputSystemFixer.cs` is permitted exactly one `FindFirstObjectByType` call -- it runs at startup before scene wiring is complete and has no alternative. All other files: no exceptions. | Stop. Replace with a `[SerializeField] private` field wired in the Inspector. | BLOCKING | Unity performance documentation |
| No public fields | Search for `public ` followed by a C# type name followed by an identifier that does not end with `(` (method) or `=>` (property). Each match is a violation. | Stop. Change to `[SerializeField] private`. Public properties and public methods are permitted. Public fields are not. | BLOCKING | Encapsulation |
| MonoBehaviour method scope | Search each non-lifecycle, non-event-handler method in a MonoBehaviour for: (1) an arithmetic operator (`+`, `-`, `*`, `/`, `%`) not inside a `string.Format`, `Debug.Log`, or `$""` interpolation call; (2) a `for`, `foreach`, `while`, or `do` loop; (3) a field of type `List<T>`, `Dictionary`, `Queue`, `Stack`, or array not tagged `[SerializeField]`. Each match is a violation. | Stop. Extract the flagged code to a new pure C# class. The MonoBehaviour method calls the new class instead. | ADVISORY | Game Programming Patterns (Nystrom) |
| Composition over inheritance | For any MonoBehaviour class declaration, count inheritance levels: `class A : MonoBehaviour` = 0 intermediate levels (permitted). `class A : B` where `B : MonoBehaviour` = 1 intermediate level (permitted for one level only). `class A : B` where `B : C` and `C` is not `MonoBehaviour` or a Unity built-in = 2+ levels, violation. | Stop. Remove the intermediate inheritance level. Replace with an interface. | BLOCKING | SOLID |
| UI structural boundary | Search any file in the `UI/` assembly for a method call on a `[SerializeField]` reference whose type is defined in `Core/` or `Managers/`, where the call has parameters or its return value is used in a computation (not a bare property read). Each match is a violation. | Stop. Replace the direct call with a `GameEvents.Raise*()` call. | BLOCKING | MVC/MVP |
| No string-based Unity references | Search the file for these exact method names immediately followed by `("`: `Animator.SetTrigger(`, `Animator.SetBool(`, `Animator.SetFloat(`, `Animator.SetInteger(`, `GameObject.Find(`, `gameObject.CompareTag(`, `Resources.Load(`. Also search for `gameObject.tag ==` or `gameObject.tag !=`. Each match is a violation. | Stop. Replace with a typed reference, a named constant, or an enum value. | ADVISORY | Unity silent-bug prevention |
| Object pooling | Search the file for `Instantiate(`. For each match, check two conditions: (1) does the same type name appear in `Instantiate(` in any other file in the project; (2) does this `Instantiate(` call appear inside a method that is an event handler or subscribed to `Update()` / `OnTick`. If either condition is true, violation. | Stop. Implement `ObjectPool<T>` for this type before writing the `Instantiate` call. | ADVISORY | Unity GC performance |
| No GC allocations in frame/tick methods | Search `Update()`, `FixedUpdate()`, `LateUpdate()`, and any method subscribed to `GameEvents.OnTick` for: `new List`, `new Dictionary`, `new [`, `.Where(`, `.Select(`, `.OrderBy(`, `.FirstOrDefault(`, `.ToList(`, `.ToArray(`, or `+` between two non-constant string operands. Each match is a violation. Note: lambda subscriptions in `OnEnable()` / `OnDisable()` are NOT a violation -- they allocate once at enable time, not per frame. | Stop. Replace with a pre-allocated cached field or a non-allocating alternative. | ADVISORY | Mobile performance standard |
| No coroutines for logic or conditions | Search each `IEnumerator` method for: (1) a `yield return` that is not `yield return null` and not `yield return new WaitForSeconds(`; (2) a call to any method that is not a Unity built-in API or a `GameEvents.Raise*()` call. Each such method is a candidate violation. | Stop. List the exact yield statements and every non-Unity method call in this coroutine. Ask the user whether it should be replaced with UniTask + event. Do not proceed without explicit approval. | ADVISORY | Unity architecture |
| Explicit initialization | Search for any `[SerializeField]` reference used in `Awake()` for any purpose other than a null check (`== null` or `!= null`). Also search for `Start()` calling a method on any `[SerializeField]` reference before calling `.Initialize()` on it. Each match is a violation. | Stop. Move the call to after `Initialize()` is invoked by the orchestrating manager. | ADVISORY | Unity lifecycle |
| Pre-write declaration (mandatory) | No detectable violation -- this is a required output before any file is written. | Before writing or modifying any `.cs` file -- regardless of change size, even a single line -- output this exact block and do not begin writing until every field is filled: `FILE: [path] \| LAYER: [Domain/Core/Managers/UI] \| NAMESPACE: [exact namespace] \| ASSEMBLY: [asmdef name] \| IMPORTS: [each using statement + which layer it comes from + whether that direction is permitted per the lookup table] \| PATTERN: [Enum / Entity / Interface / MonoBehaviour / PureC#]`. There is no size exception. | BLOCKING | Enforcement protocol |
| Post-write checklist (mandatory) | No detectable violation -- this is a required output after any file is written. | After writing or modifying any `.cs` file -- regardless of change size, even a single line -- output this checklist with each item marked pass or FAIL: `[ ] File name matches type name exactly` `[ ] Namespace matches folder path` `[ ] No using statements in forbidden direction` `[ ] Type count == 1` `[ ] All private fields begin with _` `[ ] No FindFirstObjectByType` `[ ] No public fields` `[ ] If MonoBehaviour: no arithmetic / loops / non-serialized collections in methods` `[ ] If Domain layer: zero UnityEngine using statements`. Any FAIL must be fixed before writing the next file. There is no size exception. | BLOCKING | Enforcement protocol |
| Unity MCP Inspector verification (mandatory) | A `.cs` file was written or modified that contains at least one `[SerializeField] private` field on a MonoBehaviour, AND no Unity MCP call to verify that field's wiring appears in the session output for that change. | After writing any MonoBehaviour file that adds or modifies a `[SerializeField] private` field: (1) use Unity MCP to call `get_gameobject_components` on the GameObject in the scene that holds this component; (2) check that every new `[SerializeField]` field is non-null in the returned component data; (3) if any field is null or missing, stop -- output the exact field name and expected scene path, and ask the user to wire it in the Inspector before continuing. Do not write the next file until this check passes or the user explicitly confirms the wiring will be done manually. | BLOCKING | Unity Inspector dependency injection |

---

### 4.2 Frameworks Table

| Framework | Detectable Trigger for Addition | Add Condition |
|-----------|--------------------------------|---------------|
| UniTask | An `IEnumerator` method calls any non-Unity, non-GameEvents method, OR a stop condition on coroutines fires | Add only when user explicitly requests it OR when the coroutine stop condition is triggered and the fix requires async/await |
| NSubstitute | A test file instantiates a MonoBehaviour directly to test logic that an interface could isolate | Add only when user explicitly requests it OR when the first interface-substitution test is written |
| DOTween | A method in `Update()` or a coroutine uses `Mathf.Lerp`, `Vector3.Lerp`, or `Color.Lerp` | Add only when user explicitly requests it |
| TextMeshPro | Already in project | No action required |
| Unity Input System | Already in project | No action required |
| VContainer | No trigger -- `[SerializeField]` injection is the required pattern at this project scale | Do not add unless user explicitly requests it by name |
| UniRx / R3 | No trigger -- static `GameEvents` bus is the required pattern at this project scale | Do not add unless user explicitly requests it by name |

---

> **Stop Conditions are encoded in the Classification column above.** Any row marked BLOCKING is a stop condition. The Architecture Principles Table is the single source of truth. The `/arch-review` skill reads the Classification column to sort output into BLOCKING vs ADVISORY.

---

## 5. Unity-Specific Rules

- Use C#
- Use MonoBehaviour appropriately
- Use serialized private fields, not public fields
- Do NOT modify:
  - `.unity` scene files
  - prefab files
  - ProjectSettings
  unless explicitly instructed
- Assume mobile-friendly performance constraints

---

## 6. Claude Behavior Rules

- Ask before making architectural changes
- Do NOT add features beyond scope
- Explain financial logic in simple, student-friendly terms
- When writing code:
  - Include short comments explaining intent
  - Avoid clever or dense implementations
- When unsure, propose options with trade-offs
- Never use em dashes in any output, comments, copy, or documentation

---

## 7. POC Success Criteria (Anchor All Decisions)

This POC is successful if:
1. A student can explain opportunity cost using the game (usually by pointing at a lot the rival took because the student spent cash elsewhere).
2. A student can describe compound interest effects they observed in the investing panel.
3. The student's financial decisions clearly correlate with the visible state of their chain: more profitable or more careful play produces more restaurants, higher tiers, and a healthier balance sheet than the rival. Bankruptcy (the only hard lose state) is clearly attributable to specific over-spending or under-investing decisions.

If a feature does not support these outcomes, it should be excluded.

---

## 8. Interaction Guidelines

- Design systems before writing code
- Build one system at a time
- Keep changes small and reviewable
- Optimize for learning clarity, not content volume

---

## 9. Known Issues & Deprecation Fixes

### 9.1 Namespace Conflicts
| Issue | Fix | Rationale |
|-------|-----|-----------|
| `FortuneValley.Camera` conflicts with `UnityEngine.Camera` | Renamed to `FortuneValley.CameraControl` | Unity types take precedence |

**Prevention Rule**: Never create namespaces matching Unity type names (Camera, Input, UI, Physics, etc.)

### 9.2 Input System Migration
- Project uses **New Input System** exclusively
- `InputSystemFixer.cs` auto-replaces deprecated `StandaloneInputModule` with `InputSystemUIInputModule`
- Use `Mouse.current`, `Keyboard.current`, `Touchscreen.current` - not legacy `Input` class

### 9.3 Modern Unity APIs
| Deprecated | Use Instead |
|------------|-------------|
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` |

### 9.4 Full Namespace Qualification
When inside FortuneValley namespace, use full qualification for ambiguous types:
```csharp
private UnityEngine.Camera _camera;  // Good - explicit
```
