# Vespidae Wars — Project Map

Reference index for the Vespidae Wars Unity project. Written as a lookup source for
answering questions about where things live and how systems connect.

**Repo root:** `G:\Documents\0.uni course\PostGrad\PP410_beeGame\Vespidae_Wars`
**Engine:** Unity 6000.3.20f1, URP 17.3.0, Input System 1.19.0, Cinemachine 3.1.7
**Editor instance ID (Unity MCP):** `Vespidae_Wars@abb01b1e`, bridge on `127.0.0.1:6400`

---

## 1. What the game is

Serious educational RTS about invasive wasps in South Africa's Cape Floral Region.
The player commands a **native paper wasp colony (Polistes marginalis)** against the
**invasive European paper wasp (Polistes dominula)**, with the **German wasp
(Vespula germanica)** as a secondary threat.

Core learning message: *"Identify before intervening. Not every wasp is an invasive pest."*
Core loop: Observe → Scout → Identify → Verify → Intervene → consequence → upgrade.

> **Important:** bees, pollen, honey and coin currency were **cut** from scope
> (decision 2026-07-14, GDD v2.1). Any reference to a bee colony or pollen/honey
> economy is outdated. The economy is Nectar / Prey / Fibre.

---

## 2. Directory layout

| Path | Contents |
|---|---|
| `Assets/Scripts/` | 34 runtime gameplay scripts (~10,300 lines) |
| `Assets/Scripts/Scriptables/` | 5 ScriptableObject definitions (data schema) |
| `Assets/Scripts/Editor/` | 11 editor setup/automation tools (~3,400 lines) |
| `Assets/Scripts/Controller/` | 8 legacy/other-team scripts — **not part of this stack, leave alone** |
| `Assets/Scenes/` | `Menu.unity` (build 0), `wasp RTS Lvl.unity` (build 1), `MainWorld.unity` (**orphaned, not in build settings**), `Micro/Environment.unity`, `Micro/Systems.unity` |
| `Assets/ScriptableObjectInstances/` | Authored data assets (see §5) |
| `Assets/Prefabs/` | Wasp, hive, hex and UI prefabs |
| `Assets/3D work/`, `Assets/Images . sprites/`, `Assets/Materials/` | Art assets |
| `Assets/UI/`, `Assets/Settings/` | UI assets, URP render settings |

`Assets/TextMesh Pro/` contains 34 vendor sample scripts — **not project code**, ignore them.

---

## 3. Core vocabulary (enums — the authoritative terms)

These are the exact identifiers in code. Use them when answering questions.

**`WaspFunction`** — the six worker roles (`SB_Wasps_Info.cs`)
`Scout` · `Forager` · `Builder` · `BroodCaretaker` · `Guard` · `Containment`

**`WaspScopeRole`** — faction (`SB_Wasps_Info.cs`)
`NativePlayer` · `PrimaryInvasive` · `SecondaryInvasive`

**`WaspClassification`** — `Native` · `Invasive`

**`WaspWorkforceState`** — unit activity (`WaspControl.cs`)
`Idle` · `Travelling` · `Stationed`

**`HexTile.HexState`** — territory ownership (`HexTile.cs`)
`Owned` · `Unknown` · `Scouted` · `Enemy` · `Locked`

**`HexResourceType`** — bitmask-style resource yield (`SB_Hex_Area_Info.cs`)
`None` · `Prey` · `Nectar` · `PreyAndNectar` · `Fibre` · `PreyAndFibre` ·
`NectarAndFibre` · `PreyNectarAndFibre`
Aliases exist for legacy naming: `Protein = Prey`, `Sugar = Nectar`.

**`HexTerritoryState`** — `Neutral` · `Native` · `Invasive` · `Contested`
**`HexRiskState`** — `SafeNativeHabitat` · `ContestedTerritory` · `AdvancingInvasivePressure` · `InvasiveHotspot`
**`HexVisibilityState`** — `Hidden` · `Scouted` · `Investigated`

**Resources:** Nectar (adult energy) · Prey/Protein (brood feeding) · Fibre (nest building)
**Meters:** Habitat Health · Biodiversity · Invasion Pressure

> Naming note: the Hive Training UI currently exposes Scout / Forager / Attacker,
> where **Attacker maps to Guard**.

---

## 4. Script map

### Hex / world grid
| Script | Lines | Responsibility |
|---|---:|---|
| `HexTile.cs` | 886 | Owns per-hex state, resources and scouting. `Scout()`, `Claim()`, `CaptureForEnemy(faction)`, `Gather{Prey,Nectar,Fibre}(waspCount)`, friendly/enemy wasp registries, `GetWaspFormationPosition(index, spacing, rowSpacing)` (golden-angle formation inside the hexagon) |
| `HexProgressionManager.cs` | 124 | Hex adjacency graph and reachability. `InitializeMap()`, `GetConnectedHexes`, `AreConnected`, `CanPlayerTarget`, `CanEnemyTarget`, `NotifyFriendlyClaimed` |
| `HexMouseRaycaster.cs` | 331 | **Single unified raycaster** on the Game Manager object. All click/hover picking; right-click issues group move orders |
| `HexOptionsPanel.cs` | 623 | Per-hex action panel, `Open(hex)` / `Close()`, dispatch buttons |
| `HexHoverEffect.cs` | 96 | Per-tile hover visual, `SetHovered(bool)` |
| `MapViewToggle.cs` | 27 | Macro/micro map view switch (**H** key) |

### Combat
| Script | Lines | Responsibility |
|---|---:|---|
| `HexCombatController.cs` | 576 | Per-hex battle resolution, **faction-scoped**. `NotifyOccupantsChanged()`, `IsCombatantEngaged`, `RecallFriendlyScout()`. Primary vs Secondary invasive fight each other; `MaximumAttackersPerSide` (20) caps each side |
| `WaspCombatant.cs` | 142 | Unit health/damage/attack speed, `TickAttack`, `TakeDamage`, health bar |
| `HiveCombatant.cs` | 83 | Hive health (300), `Initialize(hex, isEnemy)`, `TakeDamage`, `Eliminate`, hive health bar |

### Hive / colony
| Script | Lines | Responsibility |
|---|---:|---|
| `HiveManagement.cs` | 832 | **Central colony controller.** Static `GetOrCreate()` singleton. Skills (`TryUpgrade`, `GetEffectiveValue`), workforce counts, `TryTrainWasp`, `TryDispatchWasp`, **`TryMoveWasps`** (group move, per-role rules), `CanDispatchToHex`, colony/ecosystem meters |
| `EnemyHiveControl.cs` | 689 | Enemy faction manager — **per-faction** registration, counts, alerts, guard dispatch, skill progression, `SpawnEnemyStartup()` |
| `C_Enemy_Hive_Orc.cs` | 141 | Enemy hive marker + spawner, serialized `faction` (`WaspScopeRole`) |
| `C_Friendly_Hive_Orc.cs` | 67 | Player hive marker, `Initialize(hex, waspPrefab)`, `SpawnWasp()` |
| `C_HiveSkillsPanel.cs` | 198 | Skill tree UI. `Refresh()` builds per-stat upgrade preview (`Attack Speed 1 -> 1.25 (+0.25)`) |
| `HiveHoverEffect.cs` | 23 | Hive hover visual |

### Wasp units
| Script | Lines | Responsibility |
|---|---:|---|
| `WaspControl.cs` | 436 | Player unit: NavMesh movement, `DispatchToHex`, **`TryIssueMoveOrder`** (works while stationed), `ReturnToHomeHive`, `ReturnToIdle`, selection. `IsAvailable` includes wasps returning home |
| `EnemyWaspControl.cs` | 420 | Enemy unit: destination, alert state, threat level, `Faction` |
| `WaspControlGroupManager.cs` | 518 | **Selection + orders.** Shift+click toggle, drag box, shift+drag add, control groups 1–5, double right-click clears, `TryMoveSelectedToHex` |
| `WaspInfoPanel.cs` | 218 | Unit inspector UI |
| `WaspRoleIconBillboard.cs` | 118 | Role pictogram above a unit, `Initialize(info)` / `Refresh()` |
| `WorldHealthBarBillboard.cs` | 18 | Keeps world-space health bars facing the camera |
| `WaspInfo.cs` | 81 | Per-unit species/function data, `GetSkillValue(stat)` |

### Camera
| Script | Lines | Responsibility |
|---|---:|---|
| `C_MainWorldCameraFocus.cs` | 683 | Camera state machine: `FocusOnHex`, `FocusOnWasp`, `FocusOnHive`, `ReturnToMap`, `ZoomCloseUp`. Also owns Escape handling |
| `CameraCursorMovement.cs` | 261 | Edge/cursor panning, middle-mouse drag, `ZoomTowardsHex`, `ResetCameraPosition` |

### Navigation / UI flow
| Script | Lines | Responsibility |
|---|---:|---|
| `C_MainWorldOverlayNavigation.cs` | 1401 | **Largest runtime script.** Overlay panels, hive training, pause menu + OPTIONS screen (scroll-zoom slider and the controls list), `PauseGame`/`ResumeGame`, `BindSceneReferences()` |
| `C_MainWorldNavigation.cs` | 138 | Routes selections — `SelectHex`, `SelectWasp`, `SelectHive` (friendly/enemy overloads), `OpenSkills`, `OpenWaspInfo` |
| `C_MainWorldHUD.cs` | 215 | HUD refresh, `ShowSelectedHex(hex)` |
| `C_MainWorldSelectionDisplay.cs` | 77 | Selection readout |
| `C_MainMenu_Ctrl.cs` | 125 | Main menu, options, volume/fullscreen, quit |
| `C_WaspSelection_Menu.cs` | 148 | Species selection screen |
| `C_WaspSelectionCard.cs` | 119 | One species card |

### Tutorial
| Script | Lines | Responsibility |
|---|---:|---|
| `C_TutorialManager.cs` | 301 | `StartTutorial`, `ContinueTutorial`, `AdvanceToNextStep`, `SkipTutorial`, `CompleteTutorial`, `ResetTutorialProgress`. Blocks world input while active |
| `TutorialStep.cs` | 15 | One authored tutorial step |

### Economy
| Script | Lines | Responsibility |
|---|---:|---|
| `Resource Manager.cs` | 131 | `AddNectar/AddPrey/AddFibre`, `CanAfford(nectar, prey, fibre)`. Starting values **300 Nectar / 300 Prey / 500 Fibre**. Note the space in the filename |

---

## 5. Data model (ScriptableObjects)

| Definition | Purpose | Authored instances |
|---|---|---|
| `SB_Wasps_Info` | Species: classification, scope role, per-function info | 3 — `SO_PolistesMarginalis`, `SO_PolistesDominula`, `SO_VespulaGermanica` |
| `SB_Hex_Area_Info` | Per-hex identity, habitat, resources, starting protein/sugar, wasps present | 43 — `SO_Hex1`–`SO_Hex42` + `SO_FynbosScrub` |
| `SB_Hex_Gathering_Rules` | Shared tick interval + per-wasp yields | 1 — `SO_DefaultHexGatheringRules` |
| `SB_Wasp_Skill` | Skill costs (`WaspSkillCost`) and stat effects (`WaspSkillStat`) | 6 — one per `WaspFunction` |
| `SB_PlayerSelection_State` | Runtime player selection carrier (survives scene loads) | 1 — `SO_PlayerSelection` |

`HexTile` references **both** `SB_Hex_Area_Info` and `SB_Hex_Gathering_Rules`.
Create-asset menu paths live under `Vespidae Wars/…`.

---

## 6. Prefabs

```
Prefabs/HexTile.prefab
Prefabs/Friendly/Wasp_Player.prefab          Prefabs/FriendlyHives/Friendly_hive.prefab
Prefabs/Enemy/Wasp_Enemy_PolistesDominula.prefab
Prefabs/Enemy/Wasp_Enemy_PolistesMarginalis.prefab
Prefabs/Enemy/Wasp_Enemy_VespulaGermanica.prefab
Prefabs/EnemyHives/Enemy_hive.prefab
Prefabs/UI/WaspFunctionCard.prefab           Prefabs/Wasp_pre.prefab
```

---

## 7. Runtime wiring — who calls whom

```
HexMouseRaycaster  (one instance, on Game Manager)
        │  click / hover
        ▼
C_MainWorldNavigation ──── SelectHex / SelectWasp / SelectHive
        │                        │
        │                        ├──► C_MainWorldCameraFocus   (FocusOnHex / Wasp / Hive)
        │                        ├──► C_MainWorldHUD           (ShowSelectedHex)
        │                        └──► C_MainWorldSelectionDisplay
        │
        └──► C_MainWorldOverlayNavigation ──► OpenHiveTraining ──► HiveManagement
                                                                        │
                        HiveManagement.GetOrCreate()  ◄─────────────────┘
                                │
                                ├──► WaspControl        (spawn, train, dispatch)
                                ├──► HexTile            (scout, claim, gather)
                                └──► Resource Manager   (nectar / prey / fibre)

EnemyHiveControl ──► EnemyWaspControl   (separate enemy-side stack)
```

**Selection gating:** the player must select a hex first; the `inHexView` state then
enables wasp raycasts from the CloseUp camera. The other team member's navigation
scripts remain separate from this path.

---

## 8. Editor tooling

Menu items under **`Tools/Vespidae Wars/`**:

| Script | Lines |
|---|---:|
| `Editor/VespidaeMenuSetup.cs` | 881 |
| `Editor/VespidaeCombatProgressionSetup.cs` | 576 |
| `Editor/VespidaeControlGroupsSetup.cs` | 343 |
| `Editor/VespidaeHiveWorkforceSetup.cs` | 312 |
| `Editor/VespidaeHexDataSetup.cs` | 307 |
| `Editor/VespidaeHerbertHiveTrainingSetup.cs` | 285 |
| `Editor/VespidaeHerbertHudStyleSetup.cs` | 225 |
| `Editor/VespidaeHerbertHexOptionsSetup.cs` | 213 |
| `Editor/VespidaeHerbertResourceBarSetup.cs` | 147 |
| `Editor/VespidaePrefabSetup.cs` | 86 |
| `Editor/VespidaeHerbertPauseMenuSetup.cs` | 38 |

These are scene/data generators — they author GameObjects and assets rather than
running at play time.

---

## 9. Gotchas

- `Resource Manager.cs` has a **space in the filename**, which breaks naive globbing.
- `HexResourceType` carries legacy aliases (`Protein`/`Sugar`) pointing at
  `Prey`/`Nectar` — the same value under two names.
- Hive Training UI says **Attacker** but the underlying enum value is **`Guard`**.
- Only one `HexMouseRaycaster` should exist; duplicates cause double-picking.
- Solo memory contains **superseded** bee/pollen/honey design entries from before the
  2026-07-14 scope change. Prefer entries dated 2026-07-14 or later.
- **The scenes are saved in BINARY format.** Text-grepping a `.unity` file for object
  names silently returns nothing. Query live Unity instead — a grep miss is not evidence.
- **Changing a default in code does not change an already-serialized value.** Scene and
  prefab instances keep whatever was saved; edit them in the Inspector or via
  `SerializedObject`. This has bitten `maximumSelection` already.
- The pause menu is rebuilt every run (`pauseMenu` is not serialized). A copy saved into
  a scene would duplicate and show through as a ghost; `DestroyStalePauseMenus()` guards this.
- `Assets/Forge Horizon/.../Player.controller` logs two harmless `Broken text PPtr`
  errors — two orphaned animator transitions pointing at a deleted state. Third-party, ignored.
- `WaspControl.IsAvailable` is true while a unit is **returning home**, so it can be
  re-tasked mid-flight. `EnemyWaspControl.IsAvailable` is Idle-only — they differ deliberately.

---

## 10. Authority order for answering questions

1. **This repo's code** — authoritative for how things are actually wired.
2. **Solo memory** — authoritative for design decisions and scope.
3. **Live Unity state via MCP** — accurate but transient; label it as live state.
4. GDD files in the parent folder `PP410_beeGame\`:
   `Vespidae_Wars_GDD_Scope.md`, `Vespidae_Wars_Leveling_and_Resources_Scope.md` (v2.1).
