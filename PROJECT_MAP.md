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
| `Assets/Scripts/` | 25 runtime gameplay scripts |
| `Assets/Scripts/Scriptables/` | 5 ScriptableObject definitions (data schema) |
| `Assets/Scripts/Editor/` | 4 editor setup/automation tools |
| `Assets/Scenes/` | `Menu.unity`, `MainWorld.unity`, `wasp RTS Lvl.unity` |
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
| `HexTile.cs` | 422 | Owns per-hex state, resources and scouting. `Scout()`, `Claim()`, `Gather{Prey,Nectar,Fibre}(waspCount)`, friendly-wasp registry, `GetFriendlyWaspCount(function)` |
| `HexMouseRaycaster.cs` | 266 | **Single unified raycaster** on the Game Manager object. Handles all click/hover picking |
| `HexHoverEffect.cs` | 96 | Per-tile hover visual, `SetHovered(bool)` |
| `HexOptionsPanel.cs` | 225 | Per-hex action panel, `Open(hex)` / `Close()` |
| `MapViewToggle.cs` | 27 | Macro/micro map view switch |

### Hive / colony
| Script | Lines | Responsibility |
|---|---:|---|
| `HiveManagement.cs` | 448 | **Central colony controller.** Static `GetOrCreate()` singleton. Skill levels & upgrades (`TryUpgrade`, `GetEffectiveValue`), workforce counts, `TryTrainWasp`, `TryDispatchScout`, `SpawnFriendlyStartup`, colony/ecosystem meters |
| `C_Friendly_Hive_Orc.cs` | 51 | Player hive marker, `Initialize(hex, waspPrefab)` |
| `C_Enemy_Hive_Orc.cs` | 63 | Enemy hive marker, `Initialize(hex, waspPrefabs[])` |
| `EnemyHiveControl.cs` | 205 | Enemy faction manager — registration, per-faction counts/alerts/destinations, `SpawnEnemyStartup()` |
| `HiveHoverEffect.cs` | 23 | Hive hover visual |
| `C_HiveSkillsPanel.cs` | 105 | Skill tree UI, `Refresh()` |

### Wasp units
| Script | Lines | Responsibility |
|---|---:|---|
| `WaspControl.cs` | 264 | Player unit: NavMesh movement, `DispatchToHex`, `SetAssignedFunction`, `ReturnToIdle`, selection |
| `EnemyWaspControl.cs` | 87 | Enemy unit: destination, alert state, threat level, faction |
| `WaspInfo.cs` | 60 | Per-unit species/function data, `GetSkillValue(stat)` |
| `WaspInfoPanel.cs` | 81 | Unit inspector UI |

### Camera
| Script | Lines | Responsibility |
|---|---:|---|
| `C_MainWorldCameraFocus.cs` | 598 | **Largest runtime script.** Camera state machine: `FocusOnHex`, `FocusOnWasp`, `FocusOnHive`, `ReturnToMap`, `ReturnToPreviousView`, `ZoomCloseUp`. Also owns Escape handling |
| `CameraCursorMovement.cs` | 203 | Edge/cursor panning, `ZoomTowardsHex`, `ResetCameraPosition` |

### Navigation / UI flow
| Script | Lines | Responsibility |
|---|---:|---|
| `C_MainWorldNavigation.cs` | 125 | Routes selections — `SelectHex`, `SelectWasp`, `SelectHive` (overloaded friendly/enemy), panel open/close, `ReturnToMenu` |
| `C_MainWorldOverlayNavigation.cs` | 329 | Overlay + hive-training screens, `BindSceneReferences()`, `OpenHiveTraining(hive)` |
| `C_MainWorldHUD.cs` | 180 | HUD refresh, `ShowSelectedHex(hex)` |
| `C_MainWorldSelectionDisplay.cs` | 77 | Selection readout |
| `C_MainMenu_Ctrl.cs` | 125 | Main menu, options, volume/fullscreen, quit |
| `C_WaspSelection_Menu.cs` | 148 | Species selection screen |
| `C_WaspSelectionCard.cs` | 119 | One species card |

### Economy
| Script | Lines | Responsibility |
|---|---:|---|
| `Resource Manager.cs` | 123 | `AddNectar/AddPrey/AddFibre`, `CanAfford(nectar, prey, fibre)`. Note the space in the filename |

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

| Menu item | Script | Lines |
|---|---|---:|
| Build Menu Flow | `Editor/VespidaeMenuSetup.cs` | 881 |
| Setup Hive Workforce | `Editor/VespidaeHiveWorkforceSetup.cs` | 312 |
| Setup Hex Data | `Editor/VespidaeHexDataSetup.cs` | 204 |
| Setup Hive Triggers and Wasp Navigation | `Editor/VespidaePrefabSetup.cs` | 86 |

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

---

## 10. Authority order for answering questions

1. **This repo's code** — authoritative for how things are actually wired.
2. **Solo memory** — authoritative for design decisions and scope.
3. **Live Unity state via MCP** — accurate but transient; label it as live state.
4. GDD files in the parent folder `PP410_beeGame\`:
   `Vespidae_Wars_GDD_Scope.md`, `Vespidae_Wars_Leveling_and_Resources_Scope.md` (v2.1).
