# Not a Hero and End of Zoe integration plan

Status: Proposed; isolated weapon experiments implemented, none certified
Last updated: 2026-10-09

## Purpose

This document defines a safe, incremental path for bringing Not a Hero (`CH8`) and End of Zoe (`CH9`) enemies and items into the main campaign.

The objective is not to make every discovered DLC object selectable as quickly as possible. The objective is to establish a repeatable compatibility process in which each enemy archetype and item family is proven in isolation before it enters normal randomization pools.

This plan builds on:

- [Notes.MD](Notes.MD), especially the DLC integration and recipe notes.
- [DLCIntegrationEvidence.MD](DLCIntegrationEvidence.MD), containing the binary-specific IDA evidence behind this plan.
- [DLC weapon investigation](weapons/dlc_weapons.md), containing the source catalog, current lab boundary, live observations, and remaining AMG-Dual/player-system work.
- [DLCAndModes.MD](../.analysis/knowledge/DLCAndModes.MD).
- [ItemsInventoryRecipes.MD](../.analysis/knowledge/ItemsInventoryRecipes.MD).
- [dlc-enemy-integration-findings.md](../.analysis/dlc-enemy-integration-findings.md).
- The abandoned `origin/dlc-enemies` implementation and its runtime experiments.

## Executive conclusion

Not a Hero and End of Zoe are effectively small games embedded inside RE7. They reuse the engine and some base classes, but they have their own scene mounting, player systems, object families, managers, AI initialization, and lifecycle assumptions.

Reliable integration therefore cannot be implemented as a single `allow DLC content` switch. It must be handled as a compatibility project with:

1. Generated source manifests.
2. Explicit runtime-family metadata.
3. Minimal, typed adapters.
4. Per-archetype and per-item certification.
5. Fail-closed behavior when any prerequisite is missing.

Only certified content should enter normal randomization pools. Discovered or experimental content must remain unavailable by default.

## Why the earlier approach failed

### Content discovery is incomplete

The placement generator can see DLC scenes: the current placement data contains 203 Not a Hero placements and 182 End of Zoe placements. The item definition generator, however, scans only `natives/stm/prefab/item/`, and the weapon definition generator still has a `TODO DLCs` marker. As a result, placement discovery and usable item definitions disagree before runtime integration begins.

The current item data contains no Not a Hero or End of Zoe definitions. The current weapon data contains only main-game weapons.

### DLC assets are scene-mounted, not merely referenced

Adding a prefab, mesh, or enum value is not enough. DLC resources can depend on the standby state of folders below:

```text
natives/stm/scenes/master.scn.20
natives/stm/scenes/dlc/dlc_active_root.scn.20
```

Activating an entire DLC root is unsafe. Earlier tests caused a boot black screen before `GameManager` and activated DLC gameplay objects, including a Mama Mold at CH8 coordinates. Asset integration must copy the dependency closure and activate only individually proven support folders. Gameplay, mission, arena, and boss folders must remain inactive.

### Enemy spawning is a multi-stage runtime process

Main-game enemies normally use:

```text
app.EnemyGenerator
app.EnemyPool
app.EnemySpawnInfo
app.EnemySpawnInfoOptionEm####
```

Not a Hero enemies require:

```text
app.CH8EnemyGenerator
app.CH8EnemyPool
app.CH8EnemySpawnInfo
app.CH8EnemySpawnInfoOptionEm####
```

End of Zoe enemies require:

```text
app.EnemyGenerator
app.EnemyPool
app.CH9EnemySpawnInfo
app.CH9EnemySpawnInfoOptionEm####
```

No CH9-specific generator or pool type has been found in the current TDB.

Normal generator spawning is also deferred. `spawnInstance()` binds a pooled instance and leaves `RequestedOperation = Setup`; `setupInstance()` performs the final target, health, think, appearance, transform, and completion work. A cross-mode instance can become stranded between these stages.

Imported pooled enemy roots must also have `via.GameObject.Tag = Enemy`, or pool startup can ignore them entirely.

### Serialized component parity is not runtime parity

DLC command-action tables are populated during native awake/start processing. They are not serialized in the source scene or imported template.

The main campaign Em4400 prototype reached all of the following and still T-posed:

- The full 63-component CH8 stack.
- Correct enemy tag.
- Correct spawn position and target.
- Bound action, think, status, order, damage, and sensor references.
- A campaign-compatible spawn-info state.

Its command container and action list were both empty. A healthy Not a Hero Em4400 had 29 actions in each. Later managed reconstruction made the enemy move and attack, but exposed a second terminal-action stall. Em4600 reached 38/38 action parity and then exposed missing idle, target, and action-setup layers.

The 2026-08-25 IDA pass identified the first concrete cause. `CH8Em4400ActionController.doAwake()` contains no chapter guard: it calls `CH8MoldedActionController.doAwake()` and returns before action insertion when that base call leaves a managed exception. Both the CH8 molded base and the more general `CH8EnemyActionController.doAwake()` require the `CH8GameRankManager` singleton when an enemy rank manager is present. That singleton is absent in the campaign. Em4000, Em4200, and Em4400 use the molded path; Em4450 uses the CH8 enemy base directly.

This is narrower than the earlier hypothesis of a generic missing DLC lifecycle. The normal `EnemySpawnInfo.setupInstance()` path subsequently applies the campaign `GameManager` rank, so the first production experiment should bypass or replace only the CH8-specific awake-time rank synchronization and then allow the derived controller to construct its native action table.

This proves that successful asset loading or one successful animation is not a reliable acceptance criterion.

### Broad runtime repair is unsafe

The experiments established several hard prohibitions:

- `IObject.As<T>()` is not a runtime type check.
- Broad writes through guessed backing fields or `SetDataBoxed` can corrupt the REFramework VM.
- Direct command-controller updates can crash campaign scenes.
- Collider, damage, request-set, and lifecycle methods must not be force-started generically.
- `DoomsBehavior.Enabled = true` can retrigger lifecycle or empty pools.
- A loaded native plugin DLL remains stale until the game restarts; build and loaded hashes must be verified.

Future runtime adapters must be narrow, typed, idempotent, version-gated, and based on a healthy source-DLC trace.

### DLC items are runtime behaviors, not IDs

An item requires a complete chain:

```text
ItemData
  -> inventory ItemPrefab
  -> insert/use/equip callback
  -> world pickup or ItemResource registration
  -> messages and assets
  -> save/load and item-box behavior
```

Seeing an item name or ID in the item box proves only that lookup and messages resolve. Moving it into inventory causes `ItemManager` to instantiate the real `ItemPrefab`; using or equipping it can then call into CH8- or CH9-specific player systems.

World objects can also contain an `app.fsm.ItemAddTest` action with its own item ID. Changing only `app.Item.ItemDataID` can leave the interaction desynchronized.

## Design principles

1. **Softlock prevention beats content count.** Unsupported content is skipped.
2. **Certify individual runtime families.** A DLC label alone is not a compatibility guarantee.
3. **Prefer campaign-native behavior adapters.** Reusing a DLC ID or visual asset is safer than importing an entire DLC player subsystem.
4. **Preserve source-only fields.** Never overwrite non-null DLC setup data with null campaign data.
5. **Keep campaign request actions.** CH8 need-area queues depend on CH8 player-area state and `CH8AIWorldBlackBoard` and must not be used for campaign imports.
6. **Copy dependency closures.** Never activate a gameplay root as an asset-loading shortcut.
7. **Fail closed and report why.** Missing templates, assets, type metadata, DLC ownership, or runtime signatures must disable the entry cleanly.
8. **Determinism remains mandatory.** All selection and placement decisions continue to use stable randomizer RNG keys.

## Compatibility registry

Add a generated registry shared by DataGen, the core randomizer, tests, and runtime support. An enemy entry should describe at least:

```text
Logical enemy ID
DLC and source chapter
Display/category metadata
UnitAlias
Template/prefab alias
Runtime component-family alias
Generator type
Pool type
Spawn-info type
Concrete option type
Campaign request-action type
Source template scene
Source spawn-info scene
Support-folder allowlist
Dependency manifest
Runtime adapter ID
Placement restrictions
Support status
```

The component-family alias must be separate from `UnitAlias` because aliases can reuse another family:

- `Em4210` uses the `CH8Em4200` component and option stack.
- `Em4600` uses the `CH8Em4000` component and option stack.

An item entry should describe at least:

```text
Item ID
DLC and source settings file
Category and stack behavior
Inventory prefab
World pickup strategy
Message sources
Asset manifest
Insert/use/equip behavior family
Required player or singleton services
Ammo/weapon/recipe dependencies
Supported players and chapters
Support status
```

Support states:

| State | Meaning | Selectable normally |
| --- | --- | --- |
| `Discovered` | Source data is known but no compatibility claim is made. | No |
| `StaticReady` | Templates, messages, assets, and serialized scene output validate. | No |
| `Experimental` | A deterministic diagnostic scenario works, but the full matrix has not passed. | Explicit diagnostic option only |
| `Certified` | The complete runtime and regression matrix passes. | Yes |

The existing `allow-dlc-items` behavior must additionally require `Certified` status. An equivalent certification gate is required for enemies. New DLC entries should default to a zero selection ratio even after their metadata is generated.

## Asset integration

### Generated dependency manifests

For each imported template or item, generate a manifest containing all known dependencies:

- SCN/PFB/USER RSZ resource and userdata references.
- Meshes and material files.
- Textures.
- Motion and motion-bank resources.
- Effects and effect lists.
- Sound resources where applicable.
- Messages.
- Nested prefab and userdata dependencies.

The existing abandoned-branch dependency traversal is a useful starting point, but it must be extended beyond RSZ references. Generation should fail or mark the entry incomplete when a dependency cannot be resolved.

### Scene support-folder policy

Maintain per-entry or per-runtime-family allowlists. For example, a CH8 enemy may require an AI-map or enemy-support folder, but must not stand up `Mother_c08`, `c08_MotherLoder`, mission, arena, or boss folders.

Tests must assert that:

- DLC master/gameplay roots remain in their campaign-safe standby state.
- Only allowlisted support folders are activated.
- Required dependencies are present in the generated pak.
- Missing DLC files skip the affected entry without generating a partial object.

## Enemy integration architecture

### 1. Treat the old branch as a research archive

Use `origin/dlc-enemies` to recover:

- Enemy metadata and alias mappings.
- Typed generator/pool/spawn-info helpers.
- Template extraction logic.
- Asset dependency traversal.
- Static regression tests.

Do not merge or cherry-pick the complete implementation. In particular, the large Em4400-focused REFramework bridge should be decomposed into evidence, diagnostics, and narrowly justified adapters.

### 2. Establish healthy source-mode snapshots

Before importing an archetype, record a healthy instance inside its native DLC:

- Game build and DLC ownership.
- Source scene and spawn GUID.
- Runtime type of every component.
- Generator, pool, spawn-info, and option types.
- Relevant non-null setup fields.
- Pool and GameObject update/pause state.
- Spawn-info operation and alive/appeared/completed state.
- Action container count and registered action-list count.
- Idle and current action.
- Think root/subgoal, target, target distance, sensors, and navigation state.
- Resource, userdata, and directive references.
- Damage, death, cleanup, room-unload, and respawn transitions.

Randomization inside CH8 and CH9 should be implemented as an intermediate milestone. It validates extraction and templates while the native managers, blackboards, scene roots, player state, and lifecycle are present.

### 3. Generate complete templates

Locate a source by the combination of:

- Concrete `UnitAlias`.
- Prefab alias/path.
- Concrete runtime component and option types.

Import both the enemy template and spawn-info template. Preserve DLC-owned fields such as non-null `MapParameter`, `HealthParameter`, `spawnNeedArea`, and CH9 mission fields. Copy campaign-owned fields only when required to keep the original placement and request behavior.

Missing or ambiguous sources must be reported and skipped. Do not emit a partially populated enemy.

Known unavailable prototype sources include Em5700, Em5800, Em5850, and Em6700. Known imported CH9 sources include Em7500, Em7700, Em7800, and Em7900.

### 4. Test per-family sidecar generators

The abandoned implementation promoted an existing shared generator. That risks applying a CH8 runtime family to unrelated vanilla spawn infos in the same pool.

The preferred hypothesis to test is a dedicated sidecar generator per runtime family:

- CH8 sidecar: `CH8EnemyGenerator -> CH8EnemyPool -> CH8EnemySpawnInfo`.
- CH9 sidecar: base `EnemyGenerator -> EnemyPool -> CH9EnemySpawnInfo`.
- Preserve or deliberately remap the original spawn GUID so the campaign FSM can resolve the replacement.
- Retain base `app.fsm.EnemyGenerate` request actions in campaign scenes.

This design is not yet runtime-proven and must be tested before becoming an architectural requirement. If GUID lookup or manager registration makes sidecars unworkable, the fallback must still isolate runtime families rather than blindly promoting a mixed generator.

### 5. Use a guarded runtime state machine

A runtime adapter should progress through explicit states:

1. **Discover:** enumerate the live GameObject component array and validate actual TDB type ancestry/interfaces.
2. **Validate:** compare the runtime family, required components, resources, and source manifest.
3. **Prepare:** apply only proven pause/update/draw normalization.
4. **Wait:** allow native awake/start and generator setup to run normally.
5. **Rescue setup:** only if a validated imported spawn remains stranded in `RequestedOperation = Setup`, complete the narrow proven setup path and warp.
6. **Validate commands:** require a populated native container, matching action-list count, and a valid idle action.
7. **Validate think:** require the expected target, target distance, goal chain, sensors, and navigation state.
8. **Certify or quarantine:** expose the instance only after all gates pass; otherwise disable/despawn it and log the failed gate.

All steps must be idempotent. No adapter should perform broad per-frame repair.

### 6. Prefer a native lifecycle fix over action reconstruction

The immediate CH8 target is now known: the CH8 action-controller base performs an awake-time rank synchronization through `CH8GameRankManager`. In campaign mode, the missing singleton raises the managed error observed by the derived controller, which then returns before native action insertion.

Test a narrow campaign-only hook at the CH8 base rank-sync block that does one of the following:

1. Preserves the current `EnemyRankManager` value and skips only the CH8 singleton lookup.
2. Uses the ordinary `GameManager` rank, matching the later `EnemySpawnInfo.setupInstance()` behavior.

Do not skip the complete CH8 base `doAwake()`: it also installs rank-change handlers and binds required components. After the narrow rank fix, require the derived method to build its own command container and allow the ordinary `EnemyActionController.doStart()` path to register and set up those actions.

Any native patch must:

- Verify the exact game executable or code signature.
- Refuse to run on an unknown build.
- Be limited to imported DLC instances outside their native chapter.
- Preserve the native action set and setup sequence.
- Log its activation and result.

Managed action reconstruction is a fallback only when the exact healthy action set and setup sequence have been captured. It should be keyed by component family, not requested alias, and must never accept list-count parity without idle/current-action and per-action setup validation.

### 7. Expand by measured complexity

Create a static and runtime complexity score using:

- Component count.
- DLC singleton and player-manager references.
- Runtime-generated action count.
- Child enemies, detachable weapons, or projectiles.
- Custom messages, mission state, blackboards, or arena controllers.
- Navigation and waypoint assumptions.

Use the score and native traces to select the first cross-mode archetype. Based on static inspection, Em7800 or Em7900 may be better initial CH9 candidates than Em7500 or the bladed Em7700, but this must be confirmed by source-mode tracing. Evaluate Em4210 as an early CH8 candidate rather than assuming Em4400 or Em4600 should be generalized first.

Defer:

- Em4400 until the generic lifecycle path is understood.
- Em4450/Em4460 wrapper or child-spawn behavior until ordinary single enemies work.
- Em4500 Mutated Lucas and all scripted bosses until last.
- Any enemy requiring a DLC mission, UI, arena, or custom game-mode manager.

## Item integration architecture

### 1. Complete discovery without enabling content

Extend DataGen to scan:

```text
natives/stm/ch8/prefab/item/
natives/stm/ch9/prefab/item/
```

It must also read the DLC weapon settings, messages, item-resource scenes, inventory prefabs, pickup prefabs, and referenced userdata. Generate `Discovered` definitions for inspection, but do not make them selectable.

### 2. Separate pickup presentation from inventory behavior

For world placements, prefer a known-good campaign pickup shell with the target DLC `ItemDataID`. The real inventory prefab remains attached to `ItemData.ItemPrefab` and should not be placed directly in the world unless its interaction path is proven campaign-compatible.

When modifying an existing interact object, update both:

- `app.Item.ItemDataID`.
- Any `app.fsm.ItemAddTest` item IDs or lists used by the interaction.

Copy messages into the campaign message file and register generated pickup resources in `itemresources.scn.20`.

### 3. Prefer campaign-native semantic adapters

Where a DLC item uses a mode-specific callback but has a simple campaign meaning, preserve the item ID, presentation, values, and assets while adapting the behavior to a campaign-native component.

Examples:

- Convert a CH8 healing item to a campaign `WeaponRemedyAppend` implementation with equivalent heal values.
- Implement a DLC passive effect through `PlayerPassiveSkill` or a narrow insertion bridge instead of calling a CH8/CH9 passive manager from Ethan's inventory.
- Preserve ammo IDs only when the matching weapon consumes them correctly; otherwise keep them unavailable.

IDA confirms these are hard runtime boundaries. CH9 passive insertion casts the inventory owner to `ICH9PlayerOrder` before registering attack/guard amulets and the passive userdata. CH9 gauntlet insertion resolves `CH9PlayerEquipManager` from the owner and registers itself as a knuckle weapon. CH8 and CH9 remedy-use methods both check mode-specific player interfaces; the CH8 method also updates the CH8 rank manager's remedy-use score when that manager exists.

The Birthday skill implementation is the reference pattern: copy the full item-data/prefab/userdata/message/resource chain, synthesize a campaign-safe pickup, and bridge only the incompatible insert callback.

### 4. Roll out item families in risk order

Recommended order:

1. **CH9 `NumaItem013` First Aid Med.** It uses the base `app.WeaponRemedyAppend` and is the best initial end-to-end candidate.
2. **CH8 Med Injectors.** Adapt to campaign remedy behavior after their values are captured.
3. **Simple materials and ammunition.** Enable only with a certified consumer or recipe; otherwise they are inert.
4. **CH8 Tactical Knife.** Its core weapon component is closer to the generic campaign path than the DLC firearms.
5. **CH8 firearms and matching ammunition.** Prove equip, firing, reload, save data, upgrades, and damage behavior as one family.
6. **CH8 grenades and weapon attachments.** These rely on CH8-specific player and shell-manager behavior.
7. **CH8/CH9 passive items.** Adapt their effects to campaign player services or add narrowly scoped bridges.
8. **CH9 healing/food variants.** Either port the CH9 stack-integration system or expose independent campaign-adapted consumables.
9. **CH9 weapons and effigies.** Joe's gauntlets, throwing weapons, stake bombs, and passives depend heavily on CH9 equip, mesh, animation, input, and player-order systems and should be treated as player-system ports.

Never add CH8/CH9 progression keys, mission items, or files to generic campaign pools.

### 5. Add recipes only after item certification

Every ingredient and result must already be certified independently. Recipe generation must validate:

- All values are valid `app.ItemID` entries.
- The combine dictionary references valid recipe DataIDs.
- No duplicate dictionary entries exist.
- The visible recipe count stays within the vanilla menu limit unless the REFramework extension is enabled and tested.
- Stack conversion behavior survives inventory and save/load tests.

## Validation and certification

### Enemy static gate

- Template root uses `Tag = Enemy`.
- Prefab and all dependency-manifest files are present.
- Generator, pool, spawn-info, and concrete option types match the registry.
- Shared campaign fields are copied deliberately.
- Non-null DLC setup fields are preserved.
- Only allowlisted support folders are activated.
- Campaign request actions remain base-game actions.

### Enemy runtime gate

- `spawnInstance()` is reached.
- `setupInstance()` completes normally or through the narrow validated rescue.
- The live instance is warped to the spawn-info transform.
- `RequestedOperation = None` and operation completion matches the healthy source instance.
- Expected components and references exist.
- Command container count equals the registered action-list count.
- Idle and current actions are valid.
- Think graph, target, target distance, sensors, motion, and navigation are functional.
- The enemy acquires the player, moves, attacks, takes multiple damage types, dies, drops/cleans up, and unloads.
- Room leave/re-entry and checkpoint save/reload work.
- Multiple instances work only after one-instance certification.
- No unexplained exceptions, action stalls, crashes, or DLC gameplay-scene activation occur.

Run at least 30 spawn/attack/damage/death/cleanup cycles across:

- One open campaign area.
- One constrained interior.
- One checkpoint/save-load scenario.

Record the game build, DLC availability, generated seed/configuration, runtime bridge version, and loaded plugin hash for every certification run. Run crash-isolation tests with the REFramework MCP server stopped.

### Item runtime gate

- World pickup works and displays the correct model, icon, name, and description.
- Inventory insertion instantiates the real inventory prefab.
- Stacking and maximum counts are correct.
- Item-box transfer works in both directions.
- Use/equip behavior works for every declared supported player.
- Weapon save data, reload, ammo consumption, upgrades, and discard behavior work where applicable.
- Save/reload preserves the correct state.
- Recipe integration works where applicable.
- Missing DLC files or unsupported player contexts skip the item safely.

## Regression-test strategy

Port and expand the abandoned branch's tests to cover:

- DLC metadata and alias/component-family mappings.
- Template and spawn-info source presence.
- Missing sources producing a clean skip rather than partial output.
- CH8 generator/pool/spawn-info family selection.
- CH9 base generator/pool plus CH9 spawn-info selection.
- Campaign `EnemyGenerate` actions remaining unchanged.
- Enemy pool root tags.
- Preserved `MapParameter`, `HealthParameter`, `spawnNeedArea`, and CH9-specific fields.
- Dependency-manifest completeness.
- No activation of DLC gameplay roots.
- Support-folder allowlists.
- DLC item settings, inventory prefabs, messages, and item-resource registration.
- Certification gates excluding discovered/experimental entries from normal pools.
- REFramework artifacts being included only when a certified entry requires them.

Unit tests cannot prove AI quality or player-system compatibility. Each certified archetype or item family must therefore have a checked-in manual/runtime validation record containing the exact tested version and remaining limitations.

## Phased delivery plan

### Phase 0: Recover evidence and freeze scope

- Keep the main branch as the source of truth.
- Inventory useful files and tests on `origin/dlc-enemies`.
- Port documentation, metadata, read-only diagnostics, and tests first.
- Do not port the complete runtime bridge.
- Define the compatibility registry and support-state schema.

Exit criterion: the repository can describe known DLC content without selecting or deploying it.

### Phase 1: Deterministic extraction and diagnostics

- Add CH8/CH9 item and weapon discovery.
- Generate enemy and item dependency manifests.
- Add source-mode snapshot tooling.
- Add a campaign diagnostic scenario with one fixed location and seed.
- Add game-build, plugin-hash, and DLC-file prerequisite checks.

Exit criterion: every candidate has reproducible source evidence and missing prerequisites fail closed.

### Phase 2: First certified item vertical slice

- Integrate CH9 `NumaItem013` end to end.
- Prove pickup, inventory, use, item-box, stack, save/load, and messages.
- Use the result to finalize the item compatibility API.
- Add CH8 healing items only after the same adapter pattern is proven.

Exit criterion: one CH9 item is safe in ordinary campaign item randomization.

### Phase 3: Native-DLC enemy randomization

- Randomize one ordinary enemy within CH8.
- Randomize one ordinary enemy within CH9.
- Validate template generation and source snapshots without cross-mode lifecycle differences.

Exit criterion: one non-boss archetype in each DLC passes the runtime matrix in its native mode.

### Phase 4: First campaign enemy vertical slice

- Rank candidates by measured complexity.
- Test the per-family sidecar generator hypothesis.
- Import exactly one archetype into one deterministic campaign location.
- Trace and fix the smallest missing native lifecycle boundary.
- Refuse broad managed graph reconstruction.

Exit criterion: one archetype passes the full campaign certification matrix and is still opt-in with ratio zero.

### Phase 5: Family expansion

- Reuse the certified adapter for aliases with the same runtime component family.
- Certify each alias and placement restriction separately.
- Add multiple-spawn and enemy-multiplier testing only after single-instance stability.
- Expand item families in the defined risk order.

Exit criterion: ordinary, non-boss CH8/CH9 content can be enabled selectively without weakening existing campaign safeguards.

### Phase 6: High-risk content

- Child-spawning and wrapper enemies.
- Passives with player-manager bridges.
- CH8 attachments and throwables.
- CH9 weapons/player-system adaptations.
- Scripted bosses, arenas, mission state, and UI-dependent content.

Each remains an independent project and may be declared unsupported if it cannot meet the same reliability gate.

## Burnout and failure controls

The previous attempt became exhausting because every visible improvement exposed a new hidden runtime layer. The process should make that progression observable and bounded:

- Work on one item family or enemy archetype per vertical slice.
- Keep every experimental entry at ratio zero.
- Use deterministic scenes, placements, seeds, and logs.
- Record healthy source values before changing runtime code.
- Introduce only one crash-sensitive runtime change per test build.
- If a fix needs broad field writes, manual lifecycle ticking, or repeated per-frame repair, stop and return to native tracing.
- Treat a failed certification as useful classification, not as pressure to ship another workaround.
- Allow an explicit `Unsupported` result when an archetype depends on too much of its original game mode.

## Immediate implementation backlog

The first work package should contain only:

1. Compatibility registry and support-state types.
2. CH8/CH9 item and weapon discovery with all new entries marked `Discovered`.
3. Ported static DLC enemy tests and alias mappings from `origin/dlc-enemies`.
4. Dependency-manifest generation and support-folder allowlists.
5. Source-vs-campaign diagnostic snapshot output.
6. CH9 `NumaItem013` as the first certification candidate.

Do not add a new general-purpose runtime bridge in this work package. Runtime mutation should begin only after the source snapshot and static manifest make the exact missing lifecycle step observable.
