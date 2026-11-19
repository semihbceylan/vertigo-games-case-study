# Features and technical-review checklist

Last updated: 2026-11-19. Checked items have been implemented and covered by the
verification recorded below. Unchecked items are pending or require a complete audit;
they do not necessarily mean the feature is entirely missing.

## 1. Gameplay features and rules

- [x] Spin a reward wheel and collect rewards in the current run.
- [x] A bomb clears all current-run rewards.
- [x] Restart after loss from the configured starting zone.
- [x] Allow leaving only in safe/super zones while stopped and after presentation completes.
- [x] Guarantee exactly one bomb on normal wheels in code.
- [x] Choose the bomb slot per zone through injected randomness; repeated slots are valid.
- [x] Keep safe and super wheels bomb-free.
- [x] Configure safe/super intervals as 5/30 by default, with super priority.
- [x] Progress reward content/pools as well as quantities.
- [x] Preserve configured final-zone completion and reward banking.
- [x] Remove revive/continue gameplay, UI, pricing and related subscriptions/tests.
- [x] Keep run loot separate from persistent currency balances.
- [x] Support multiple currencies, persistence, real spending and insufficient-funds handling.
- [x] Settle configured currencies only on successful run completion/cash-out.
- [ ] Add a player-facing spending interaction without reintroducing paid continuation.

## 2. Architecture feedback

- [x] Keep gameplay in a Unity-independent, testable core assembly.
- [x] Adapt ScriptableObject configuration into core data.
- [x] Remove `SessionPhase`; use one authoritative state-object flow machine.
- [x] Gate state-specific input and reject stale/duplicate presentation completions.
- [x] Use a session-scoped, generic, type-safe event bus for cross-system notifications.
- [x] Use direct calls for commands; retain local callbacks for buttons/completions.
- [x] Separate reward catalogue, HUD projection and wheel visual projection.
- [x] Extract session presentation from `GameController` into `SessionPresenter`.
- [x] Separate wheel transitions and reward flight/showcase presentation.
- [x] Extract successful-run currency settlement and verify subscription lifecycle.
- [ ] Finish interface-boundary audit; remove remaining unnecessary concrete view coupling.
- [ ] Finish composition/controller responsibility and dead-API audit.

## 3. Data and animation feedback

- [x] Configure zone matching, priority, wheel, bomb policy, exit permission and visuals in assets.
- [x] Share zone rules between gameplay and UI instead of hardcoded safe/super checks.
- [x] Use one wheel-building path and reject invalid reward pools.
- [x] Configure reward currency mapping, category, scaling and showcase behavior explicitly.
- [x] Introduce animation profiles for spin, transitions, flights, rows and modal/showcase effects.
- [ ] Audit every tween against its profile, including delays, easing, targets, colors and offsets.
- [ ] Audit configuration validation (`Min`/`Range`) and unexplained tuning values.
- [ ] Confirm all supported new rewards, wheel tiers and zone rules need no new ID/type switches.
- [ ] Complete shared-asset runtime-state audit.

## 4. UI feedback

- [x] Validate all 31 sliced scene Images have nonzero borders within sprite dimensions.
- [x] Validate authored Simple scene Images preserve aspect ratio.
- [x] Hide and disable header, rewards, controls and zone maps under all three modal views.
- [x] Restore previous HUD visibility/interaction state rather than force-enabling controls.
- [x] Keep feedback input blocking through fade-out and prevent obsolete modal handoffs.
- [x] Add horizontal quantity-label padding in results tiles.
- [x] Inspect gameplay, loss and populated results at 20:9, 16:9 and 4:3 in the Editor.
- [ ] Repeat final-resolution/device layout checks, including long quantities and scrolling.
- [ ] Complete hierarchy naming audit: general-to-specific names with a UI type token.
- [ ] Apply `_value` to every runtime-changing element and remove it from static elements.
- [ ] Finish all-prefab sprite/border, overflow and clipping audit (scene tests are not enough).
- [ ] Reconfirm Canvas Scaler Expand, TextMeshPro and empty persistent UI calls after final migration.

## 5. References, lifecycle and performance feedback

- [x] Replace runtime progress-strip discovery/repair with 13 authored nodes.
- [x] Use typed reusable wheel slice slots and verify reuse across zone changes.
- [x] Use authored button/vignette targets rather than runtime component discovery.
- [x] Pair button and presentation listeners with enable/disable lifecycle.
- [x] Pool/prewarm reward rows, results tiles and flying icons.
- [x] Settle interrupted row scale/alpha without growing the pool.
- [x] Cancel owned modal/showcase/feedback/transition tweens and stale callbacks in tested flows.
- [x] Release and rebuild owned vignette sprite/texture resources.
- [ ] Complete repository-wide runtime `GetComponent`/child-name discovery audit.
- [ ] Complete serialized scene/prefab reference validation and missing-reference editor checks.
- [ ] Complete tween ownership/destruction and resource-disposal audit.
- [ ] Profile state-change allocations; eliminate remaining hot-path LINQ/collections/discovery.

## 6. Code standards feedback

- [x] Use block-bodied members throughout first-party runtime scripts.
- [x] Share grouped-integer reward quantity formatting.
- [x] Replace obsolete TMP wrapping property in wheel layout.
- [ ] Complete private-field `_camelCase`, constant `SCREAMING_SNAKE_CASE` and boolean Is/Has audit.
- [ ] Complete Unity-aware null-check audit; avoid `?.`/`??` on Unity objects.
- [ ] Replace remaining long-lived/tween lambdas with named callbacks where applicable.
- [ ] Remove unused enums, methods and compatibility shims after checking consumers.
- [ ] Review meaningful regions and appropriately scoped technical constants.
- [x] Leave third-party source conventions unchanged.

## 7. Verification and delivery

- [x] Latest full EditMode suite: **90/90 passed**.
- [x] Latest full PlayMode suite: **13/13 passed**.
- [x] Cover bomb clearing, exit gating, bomb invariants, deterministic selection and pool progression.
- [x] Cover state input/completion gating, economy spending/persistence and subscriptions.
- [x] Add PlayMode coverage for presentation interruption, modal handoff and pooled slot reuse.
- [x] Update README with measured test counts and presentation architecture.
- [x] Confirm installed Android modules are detected by Unity (`androidSupported = true`).
- [ ] Build a non-development Android IL2CPP APK. First attempt reported success but
      produced no APK; investigate `Build target 'Android' not supported` in the running Editor.
- [ ] Inspect build report, APK metadata/architecture and build warnings/errors.
- [ ] Install and smoke-test APK on an Android device/emulator; verify touch, loss/restart,
      safe-zone exit, results, persistence and pause/resume.
- [ ] Review `.vscode/`, template scenes and unused packages before any authorized removal.
- [ ] Pin remaining git dependencies to fixed versions/commits where required.
- [ ] Finalize README/build instructions and refresh public screenshots for the final revision.
- [ ] Decide which local `Captures/` evidence belongs in version control; keep unreviewed captures out.
- [ ] Merge only when authorized.
- [ ] Publish a normal GitHub Release only after Android validation and explicit release authorization.

## Current verification evidence

- Branch: `feature/technical-review-revision`; last verified code commit: `4f7e200`.
- EditMode job `d8c39d8c18c24118a9e2bf16a83d0292`: 90 passed, 0 failed.
- PlayMode job `5b1da582278541bab402ab6179ac7c75`: 13 passed, 0 failed.
- Android build job `build-b38ef4ca58`: tooling reported success in 47.2 seconds with
  2 errors and 11 warnings, but the expected APK does not exist. Do not treat this as a
  validated build. Console includes `Build target 'Android' not supported`; warnings concern
  compressed source textures packed into a SpriteAtlas. Module support query returns true,
  so restarting the Editor after the module installation may be necessary.
- Existing user modifications to `ProjectSettings/ProjectSettings.asset` are excluded from commits.
- No release or merge has been authorized or performed for this revision.
