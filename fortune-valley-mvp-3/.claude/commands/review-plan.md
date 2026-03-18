Review this plan thoroughly before making any code changes. This is a Unity game project — evaluate everything through the lens of game development, runtime performance, and the Unity editor workflow. For every issue or recommendation, explain the concrete tradeoffs, give me an opinionated recommendation, and ask for my input before assuming a direction.

My engineering preferences (use these to guide your recommendations):
- DRY is important — flag repetition aggressively.
- Well-tested code is non-negotiable; I'd rather have too many tests than too few.
- I want code that's "engineered enough" — not under-engineered (fragile, hacky) and not over-engineered (premature abstraction, unnecessary complexity).
- I err on the side of handling more edge cases, not fewer; thoughtfulness > speed.
- Bias toward explicit over clever.
- Favor event-driven systems over Update() polling.
- Use ScriptableObjects for tunable data — not hardcoded values.
- Keep systems loosely coupled and testable.
- Code must be readable by a junior Unity developer.

## 1. Architecture review (Unity-specific)
Evaluate:
- MonoBehaviour responsibilities — are scripts doing too much? Should logic move to plain C# classes or ScriptableObjects?
- Component boundaries — are systems properly separated (e.g., input, game logic, UI, data) or tangled together?
- Dependency graph — are there hidden couplings via FindObjectOfType, singletons, or static references? Flag any use of deprecated APIs (FindObjectOfType → FindFirstObjectByType, legacy Input → New Input System).
- Event system design — are UnityEvents, C# events, or a custom event bus used consistently? Flag any Update() polling that should be event-driven.
- Scene hierarchy and prefab structure — are GameObjects organized for clarity? Are prefab overrides clean or bloated?
- Data architecture — are ScriptableObjects used appropriately for configuration? Is runtime state properly separated from authored data?
- Namespace organization — flag any namespace conflicts with Unity types (Camera, Input, UI, Physics, Debug, etc.).

## 2. Code quality review (Unity-specific)
Evaluate:
- Code organization and folder/namespace structure against Unity conventions.
- DRY violations — be aggressive here. Flag duplicated MonoBehaviour logic, copy-pasted UI code, repeated serialization patterns.
- Serialization hygiene — are fields using `[SerializeField] private` instead of `public`? Are there exposed fields that should be read-only at runtime?
- Error handling and null safety — flag missing null checks on GetComponent, Find calls, and serialized references. Call out anywhere a missing reference would cause a silent failure or NullReferenceException at runtime.
- Coroutine and async patterns — are coroutines cleaned up on disable/destroy? Any fire-and-forget risks?
- String-based lookups — flag any use of GameObject.Find(string), SendMessage, or Animator.Play(string) that should use direct references, hashed IDs, or events.
- Technical debt hotspots — what's the most fragile code that will break first when features are added?

## 3. Test review (Unity-specific)
Evaluate:
- EditMode test coverage — are core systems (investment logic, currency, AI decisions) tested without requiring Play mode?
- PlayMode test coverage — are integration flows (buy lot, sell investment, UI interactions) covered?
- Test quality — are assertions checking meaningful outcomes, not just "no exception thrown"?
- Missing edge case coverage — zero currency, max investments, rapid input, simultaneous events, time-boundary conditions.
- Untested failure modes — what happens when a ScriptableObject reference is null? When an event fires with no listeners? When a system is accessed before initialization?
- Mock/stub patterns — are tests properly isolated or do they depend on scene state and execution order?

## 4. Performance review (Unity-specific)
Evaluate:
- GC allocations in hot paths — flag any per-frame allocations (string concatenation, LINQ in Update, new List/Array in loops, boxing). These cause GC spikes and frame hitches.
- Update/LateUpdate/FixedUpdate usage — are there scripts running per-frame logic that could be event-driven or timer-based instead?
- UI rebuild triggers — are UI elements (Text, Layout Groups, Canvas) being dirtied unnecessarily every frame? Flag any SetText/SetActive calls in Update.
- Draw calls and batching — are materials shared where possible? Are there unnecessary dynamic batching breakers (different materials, non-uniform scale)?
- Physics and raycasting — are raycasts happening every frame? Are layer masks used to limit collision checks?
- Object pooling opportunities — are GameObjects being Instantiate/Destroy'd frequently when they could be pooled?
- Memory — are there large textures, uncompressed audio, or assets loaded that are never unloaded?
- Mobile constraints — assume mobile-friendly performance. Flag anything that would struggle on a mid-range phone.

## For each issue you find
For every specific issue (bug, smell, design concern, or risk):
- Describe the problem concretely, with file and line references.
- Present 2-3 options, including "do nothing" where that's reasonable.
- For each option, specify: implementation effort, risk, impact on other code, and maintenance burden.
- Give me your recommended option and why, mapped to my preferences above.
- Then explicitly ask whether I agree or want to choose a different direction before proceeding.

## Use installed skills and plugins
Leverage these tools proactively during the review — don't wait for me to ask:
- **unity-developer** skill: Use this for any Unity performance questions, game mechanic evaluations, URP/rendering concerns, and cross-platform build considerations. Invoke it whenever the review touches Unity-specific best practices.
- **code-review** plugin: Use `/code-review` to perform structured code review on changed files or PRs when evaluating code quality.
- **code-simplifier** plugin: After identifying over-engineered or overly complex code, use this to suggest concrete simplifications.
- **github** plugin: Use for any PR, issue, or branch context needed during the review.
- **find-skills** skill: If the review surfaces a problem domain where a specialized skill might exist (e.g., testing frameworks, CI/CD, specific Unity subsystems), check if there's a skill available before recommending a manual approach.

## Workflow and interaction
- Do not assume my priorities on timeline or scale.
- After each section, pause and ask for my feedback before moving on.
- Use the Unity MCP tools (read_console, manage_scene, find_gameobjects, etc.) to verify claims about scene structure, component setup, and runtime errors — don't guess.

## BEFORE YOU START:
Ask if I want one of two options:
1. **BIG CHANGE**: Work through this interactively, one section at a time (Architecture → Code Quality → Tests → Performance) with at most 4 top issues in each section.
2. **SMALL CHANGE**: Work through interactively ONE question per review section.

## FOR EACH STAGE OF REVIEW:
Output the explanation and pros/cons of each stage's questions AND your opinionated recommendation and why, then use AskUserQuestion. NUMBER issues (1, 2, 3...) and give LETTERS for options (A, B, C...). When using AskUserQuestion, make sure each option clearly labels the issue NUMBER and option LETTER so the user doesn't get confused. Make the recommended option always the first option.
