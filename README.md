# Wheel of Fortune 🎡

A mobile **wheel-spin gambling game** built in Unity for the **Vertigo Games game-developer case study**.

Instead of picking cards, the player **spins a wheel** at every zone. Each wheel holds several
rewards and — on normal zones — a single **bomb**. Spin to grow your loot, or walk away and bank
it before the bomb wipes it all out. Rewards get better the deeper you go.

---

## 🎥 Gameplay video

<!-- Add your gameplay video link below -->
> _Gameplay video: **[ADD LINK HERE]**_

---

## 📸 Screenshots

> _Drop the captures into a `Screenshots/` folder in the repo root; the embeds below will pick them up._

| 20:9 | 16:9 | 4:3 |
|:---:|:---:|:---:|
| ![20:9](Screenshots/gameplay_20_9.png) | ![16:9](Screenshots/gameplay_16_9.png) | ![4:3](Screenshots/gameplay_4_3.png) |

<!--
Recommended set:
  Screenshots/gameplay_20_9.png   (e.g. 2400x1080)
  Screenshots/gameplay_16_9.png   (e.g. 1920x1080)
  Screenshots/gameplay_4_3.png    (e.g. 1024x768)
Optional extras: super-zone spin, bomb screen, results screen.
-->

---

## ▶️ How to play

1. **SPIN** — the wheel stops on a slice. Rewards are added to your loot; a bomb ends the run.
2. **LEAVE** — cash out and bank everything you have collected so far (available whenever the
   wheel is stopped and you have something to lose).
3. **RESTART / REVIVE** — after a bomb you can restart a fresh run or revive to keep your loot.

### Zones

| Zone | Wheel | Contains a bomb? |
|---|---|---|
| Normal (every zone) | Bronze | ✅ one bomb |
| **Safe** (every 5th zone) | Silver | ❌ risk-free |
| **Super** (every 30th zone) | Golden | ❌ risk-free, special rewards |

Reward amounts scale up with every zone, so pushing deeper is worth more — and hurts more if you
bust. The run finishes at the final zone, banking everything collected.

---

## 🛠️ Tech / requirements

- **Unity 6000.3.23f1**
- **Universal Render Pipeline (2D)**
- **DOTween** (vendored under `Assets/Plugins/Demigiant`)
- **TextMeshPro**
- **Sprite Atlas** for UI/icon batching
- **Input System** (UI module wired for the new input handler)

The project opens directly — dependencies are vendored, no extra package installs required.

---

## 📦 Build & release

- Platform: **Android** (Min SDK 25, ARM64)
- The latest `.apk` is attached to the **GitHub Releases** section of this repository.

> Build from Unity: `File ▸ Build Settings ▸ Android ▸ Build`, or via the CLI with `-executeMethod`.

---

## 🧱 Project structure

```
Assets/
├─ _Game/
│  ├─ Scripts/
│  │  ├─ Core/     → framework-free gameplay (state machine, zone rules, wallet, wheel model) + asmdef
│  │  ├─ Data/     → ScriptableObjects + factory bridging data → core
│  │  ├─ Game/     → GameController (composition root)
│  │  ├─ UI/       → HUD, feedback, results, rewards list, showcase
│  │  ├─ Wheel/    → wheel layout + spin animation
│  │  └─ Editor/   → editor tooling (atlas builder, reward categorizer)
│  ├─ Data/        → GameConfig, Zones, Wheels, RewardPools, Rewards (ScriptableObjects)
│  ├─ Prefabs/     → wheel slice, reward row, zone node
│  ├─ Art/         → UI sprite atlases
│  ├─ Audio/       → SFX
│  ├─ Scenes/      → Game.unity
│  └─ Tests/       → EditMode unit tests
├─ Art/            → source sprites (UI + icon atlases)
├─ Plugins/        → DOTween
└─ TextMesh Pro/
```

---

## 🏗️ Architecture

The codebase is split so gameplay logic is **testable without Unity** and the views stay thin.

- **`Vertigo.WheelOfFortune.Core`** — a pure-C# assembly with no `UnityEngine` dependency:
  `GameSession` (a state machine: `Idle → Spinning → Bust/Completed`), `ZoneClassifier`,
  `RewardWallet`, `ZoneWheel`/`ZoneWheelBuilder`, and an `IRandomSource` abstraction so
  randomness is deterministic and injectable.
- **ScriptableObjects** drive all content: `GameConfiguration`, `ZoneConfiguration`,
  `WheelDefinition` (queue-based *or* explicit per-slice authoring), `RewardPool`, `RewardDefinition`.
  Designers tune everything from the inspector; runtime state never lives in assets.
- **`WheelTierFactory`** adapts the ScriptableObject data to the core (`IZoneWheelFactory`) —
  a clean seam between configuration and simulation.
- **SOLID**: single-responsibility components, dependency injection into `GameSession`
  (`IRandomSource`, `IZoneWheelFactory`), interface-driven randomness, and views that depend on a
  facade (`GameScreenView`) rather than on each other.
- **UI wiring in code**: button listeners are registered in `OnEnable` (no inspector `OnClick`),
  and references auto-resolve in `OnValidate`, so prefabs/scenes stay self-healing.
- **DOTween** powers the wheel spin (spin + magnetic spring snap), reward fly-to-slot, zone-bar
  slide, and panel fades; the outcome is decided *before* the animation so the result can never change.

### UI notes

- **Canvas: Scale With Screen Size / Expand** (reference 1920×1080) with anchored layouts, so the
  UI is safe across **20:9, 16:9 and 4:3**.
- TextMeshPro throughout; changeable elements are suffixed `_value`; naming goes root → specific
  (`ui_image_spin_silver`).
- Decorative images have **Raycast Target** disabled; frames use **sliced** sprites.

---

## ✅ Tests

Edit-mode unit tests cover zone classification, the wallet, wheel building and the full game
session state machine:

```
Assets/_Game/Tests/EditMode/GameplayTests.cs   (35 tests)
```

Run them from **Window ▸ General ▸ Test Runner ▸ EditMode**, or via the CLI with `-runTests`.

---

## 👤 Author

**Semih B. Ceylan**

<!-- Built for the Vertigo Games — Game Developer case study. -->
