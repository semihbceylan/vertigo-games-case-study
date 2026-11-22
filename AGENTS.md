# Wheel of Fortune Revision Guide

## Communication and scope

- Communicate with the user in English.
- This revision addresses the technical review of the Wheel of Fortune case study.
- Work incrementally; keep the game playable and reviewable between milestones.
- Do not modify third-party sources to enforce first-party code conventions.
- Preserve existing user changes. At the initial review, `ProjectSettings/ProjectSettings.asset` was already modified.
- Do not publish releases, push branches, or remove project assets/packages without the user's authorization for that scope.

## Gameplay contract

- A bomb clears all rewards collected in the current run. After loss presentation, restart from the configured starting zone.
- Leaving is allowed only in a safe or super zone while the wheel is stopped and presentation transitions are complete.
- Every normal wheel must contain exactly one bomb, guaranteed by code rather than authored slice flags. Select its slot again for each zone using injected randomness; coincidental repeated slots are valid.
- Safe and super wheels contain no bombs.
- Default data: every fifth zone is safe, every thirtieth is super. Super takes priority when rules overlap. These intervals belong in configuration, not hardcoded gameplay/UI checks.
- Progression changes reward pools/content as well as quantities.
- Revive (continue) is paid and limited (user decision, 2026-11-22; supersedes the earlier removal). It spends a configured persistent currency, scales cost by zone, is rejected when the balance is insufficient, is capped per run, and restores the rewards the bomb cleared. Restart without reviving remains free.
- Keep run rewards separate from persistent currency balances. Support multiple currencies and a real spending operation (revive spends it).
- Preserve the configured final-zone completion behavior unless explicitly changed.

## Architecture

- Preserve a Unity-independent, testable gameplay core. Adapt ScriptableObject configuration into core data rather than introducing Unity dependencies into the core assembly.
- Remove `SessionPhase`; do not replace it with another flow enum.
- Use one authoritative flow state machine with state objects and explicit transitions. Do not introduce a parallel collection of controller flow flags.
- State-specific behavior belongs in states, not central switches or chains of state-type checks.
- Keep session data/rules separate from presentation. Split composition, session presentation, wheel transitions, and reward-fly presentation into focused responsibilities.
- Use interfaces at system/view boundaries. Wire Unity object references through serialized fields; do not use runtime scene discovery to obtain interfaces.
- Use a session-scoped, generic, type-safe event bus for cross-system notifications. It must not become a hidden flow controller or global service locator.
- Use direct interface calls for commands with a known owner. Local button notifications and operation-completion callbacks may retain Actions/delegates.
- Subscription ownership must be explicit; pair Subscribe and Unsubscribe correctly and test repeated enable/disable cycles.

## Data-driven content and animation

- New rewards, wheel tiers, and zone types within the supported rule model must be configurable through assets without adding ID lists, enum cases, or type switches.
- Reward IDs identify content only; never infer category, showcase behavior, scaling, or eligibility from ID text/prefixes.
- Zone rules are the shared source for gameplay and UI: matching conditions, priority, wheel, bomb policy, leave permission, and visuals.
- Use one wheel-building path. Reject invalid data rather than silently substituting an `empty` reward or unrelated pool.
- All tween animations use ScriptableObject animation profiles. Store applicable durations, delays, easing, target values, offsets, scales, and colors in those profiles, with appropriate Min/Range validation.
- Do not merely move animation literals into separate per-component serialized tuning fields.
- Runtime mutable state must not be stored in shared configuration assets.
- Eliminate unexplained tuning values. Put design values in configuration and fixed technical meanings in appropriately scoped named constants; avoid one unrelated global constants dumping ground.

## Unity references and lifecycle

- `GetComponent` and all variants are allowed only inside `OnValidate`, not runtime methods or helpers called outside it.
- No runtime child lookup by name. Serialize references in scenes and prefabs; migrate those assets alongside scripts.
- Editor wiring helpers, if needed, must remain editor-only and must not conceal runtime discovery.
- Validate missing references in the editor rather than adding runtime repair fallbacks.
- Every tween and sequence must be killed when its owner is destroyed, or linked correctly to its owner. Define interruption behavior so cancelled animations cannot advance obsolete flows.
- Prefer named methods over lambdas for DOTween and long-lived callbacks.
- Check Unity objects with Unity-aware boolean checks, such as `if (obj)` / `if (!obj)`. Do not use `?.` or `??` on Unity objects.
- Destroy owned runtime resources such as dynamically created materials.
- Pool reward rows/slots and flying icons. Avoid LINQ, new collections, and component discovery in state-change hot paths.

## UI contract

- Every sprite used by a Sliced Image must have a meaningful nonzero 9-slice border.
- Do not distort sprites: use appropriate slicing or preserve aspect ratio.
- Panel content must neither overflow nor be clipped at frame edges.
- Modal panels must hide and disable interaction with underlying HUD controls.
- Use `_value` only for runtime-changing UI elements, and apply it to all runtime-changing elements.
- UI object names go from general to specific and include a type token, e.g. `ui_image_rewards_background`.
- Preserve Canvas Scaler Expand mode, TextMeshPro, empty persistent UI calls, and OnValidate wiring.
- Verify gameplay, loss, and results layouts at 20:9, 16:9, and 4:3.

## First-party C# conventions

- Private fields: `_camelCase`; constants: `SCREAMING_SNAKE_CASE`; boolean names: Is/Has prefix (for private fields, `_is...` / `_has...`).
- Use block-bodied members rather than expression-bodied members.
- Group code with restrained, meaningful `#region` blocks.
- Share number formatting and genuinely reusable UI helpers. Do not preserve runtime `FindDeep` discovery merely by moving it into a utility.
- Remove unused enums, methods, and obsolete compatibility shims after updating their consumers.

## Delivery order and verification

1. Gameplay rules and regression tests, with the minimum data foundation required for correctness.
2. UI borders, aspect ratios, overflow, modal behavior, and naming.
3. State machine, responsibility split, event bus, and persistent multi-currency economy.
4. Animation ScriptableObjects and complete scene/prefab reference migration.
5. Lifecycle, pooling, allocations, code standards, and dead-code cleanup.
6. Repository hygiene, documentation, Android validation, and authorized release delivery.

- This order is a roadmap, not authorization to implement every milestone at once.
- Prefer a feature branch and small focused commits when git operations are authorized. Do not rewrite existing history.
- Before each milestone is complete: compile successfully, run relevant tests, verify serialized wiring, and exercise the affected gameplay/UI flow.
- Required regressions include bomb clearing, restricted exit, one/no-bomb invariants, deterministic slot selection, reward-pool progression, state input gating, currency spending/persistence, and subscription lifecycle.
- Add PlayMode coverage for presentation/lifecycle behavior where appropriate, in addition to pure-core EditMode tests.
- For Unity tooling, inspect available custom tools and related editor resources before mutations; check compilation/console errors after script changes.
- Report tests actually run and their results; do not claim verification based on inspection alone.
- README must describe the final implementation accurately, including rules, architecture, listener lifecycle, and the measured test-case count.
- Review `.vscode/`, template scenes, and unused packages before removal; preserve required dependencies and pin git dependencies to fixed versions/commits.
- The final Android APK should be validated before publishing an authorized normal GitHub Release, not a pre-release.
