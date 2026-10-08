# Adding spawn groups

A spawn group controls when a set of enemies appears, pauses, returns, or finishes. Author its rules in **SpawnGroups**, then assign enemies to that name in a placement tab. Spreadsheet edits take effect only after generating and installing an output that includes them.

Start with a position or delay trigger. **Neither needs an FSM state.** For story-dependent triggers, use the [state reference](spawn_group_states.md): it supplies exact text to paste, the owning scene, and the evidence for each entry. Do not derive state names from chapter names or the Uvar list.

## Sheets and references

| What you need | Where to find it |
| --- | --- |
| Group rules | [SpawnGroups](https://docs.google.com/spreadsheets/d/1YNdX9LWrhh6KDKd8Mx7JpTCMq8XY8u6BfX20YYNx9jk/edit?gid=154763280#gid=154763280) |
| Existing enemy slots | [Enemy Placements](https://docs.google.com/spreadsheets/d/1YNdX9LWrhh6KDKd8Mx7JpTCMq8XY8u6BfX20YYNx9jk/edit?gid=2063646676#gid=2063646676), `SpawnGroup` column T |
| New enemy locations | [Extra Enemy Placements](https://docs.google.com/spreadsheets/d/1YNdX9LWrhh6KDKd8Mx7JpTCMq8XY8u6BfX20YYNx9jk/edit?gid=2063983386#gid=2063983386), `SpawnGroup` column O |
| Room descriptions and exact scene paths | **Areas** in the same workbook, or the checked-in [area list](../../src/Biohazard.BioRand.RE7/_Data/areas.json). The workbook tab may be hidden; use the file if you cannot access it. |
| Values to paste into `State` | [State reference](spawn_group_states.md) and its CSV catalogue |
| Story flags and progression counters | [Uvar reference](../UvarVariables.MD); these are **not supported** in `State` |

Use a build that supports spawn groups and its complete generated REFramework files. The embedded group sheet is initially empty. The format is `Name,Parameter,State,X,Y,Z,Radius,Time,Notes`.

## First group: one enemy, no state lookup

1. Choose one ordinary Molded spawn in the room you want to test, using **Enemy Placements**. Keep its existing `Guid` and `SceneFile`. It must have `IsSpawnInfo = TRUE`, an empty `Dlc`, and no `preserve` or `cull` tag. Avoid scripted bosses and flashbacks.
2. Put `MyFirstEncounter` in that row's `SpawnGroup` cell. A placement belongs to only one group. Names are trimmed and matched without case sensitivity. Clear this cell to remove an existing placement from group control; its `Enabled` column does not disable group registration.
3. Add the following row to **SpawnGroups**. Empty CSV fields mean empty cells; do not type the word `blank`.

```csv
Name,Parameter,State,X,Y,Z,Radius,Time,Notes
MyFirstEncounter,spawn,,,,,,3,Start three gameplay seconds after a member loads
```

4. Generate using the updated data and install the complete output as described [below](#generate-and-install-your-changes). For this existing-enemy test, turn **Randomize Enemies** off, set **Extra Enemy Placements** to `0%`, and keep **Enemy Multiplier** at `1`. This keeps the chosen Molded and avoids adding unrelated extras; group control still applies.
5. Test from before this enemy slot has spawned or completed. When one of the group's native members becomes available, the three-second delay starts. Close the pause menu to let it run. An already spawned or completed slot restored from a save does not start a fresh encounter.

This first test establishes that the placement, data update, installation, and membership work. It is **not a room-entry trigger**: pools can load before the player enters the room. Replace its trigger with a measured position for the actual encounter.

## Get coordinates and choose a scene

In RE7, stand at the point where the **player** should trigger the encounter. Open the REFramework overlay and expand **BioRand 7**. Its **Position** line gives player world `X, Y, Z`; **Chapter** shows the current chapter. Record these for the trigger. Repeat separately at the point where you want to place an extra enemy. Trigger and enemy positions are different inputs.

Keep the measured `Y`; it is height. A trigger is a 3D sphere, not a flat circle. Start with a radius such as `3` and check the actual route in game; a radius of `1` is easy to miss. A large radius can reach through walls or onto another floor. Use a decimal point in CSV values.

For an **existing** enemy, retain its placement's scene path. For an **extra**, search the area list by room description and copy its exact `Path` into `SceneFile`; check that the scene loads with the encounter. A valid path alone does not establish the correct room or navigation surface. The runtime's player chapter number does not identify a room's file.

For example, the terrace file is:

```text
natives/stm/environment/scene/chapter3/c03_mainhousoutsideterrace1f.scn.20
```

Preserve `mainhousoutside` exactly. `c03_MainHousOutsideTerrace1F_sm` is an internal folder, not another `.scn.20` file. Do not append `_sm`, fix native spelling, or construct a path from a room label.

### Add a new enemy location

Fill one row in **Extra Enemy Placements**:

| Field | First-test value |
| --- | --- |
| Enabled | `TRUE` |
| Id | Empty |
| Include | `MoldedQuick` for one known generator-backed type |
| Exclude | Empty |
| Comment | Your room and purpose |
| SceneFile | Exact path from the area list |
| Chapter | The matching campaign chapter, such as `3`; this selects the generator, not a story gate |
| PosX, PosY, PosZ | Measured enemy position on traversable ground |
| RotX, RotY, RotZ, RotW | `0, 0, 0, 1` for an identity rotation; these are quaternion components, not degrees |
| SpawnGroup | Your group's name |

**Extra Enemy Placements** in the generation profile controls how many enabled extra rows are selected. `100%` (`extra-enemy-amount = 1.0`) selects all eligible rows before other constraints; lower values can omit your test. This applies to every enabled extra row, so use a small local data snapshot for an isolated test. Scene limits, excluded types, and empty enemy pools can still suppress rows. Check the generated manifest rather than assuming a row was selected.

Blank `Id` with an explicit `Include` bypasses configured enemy ratios, making it useful for a reproducible first test. `Id = random` instead filters the configured pool. Include/Exclude use whitespace-separated canonical IDs, such as `MoldedQuick MiaChainsaw`; `Mia` is not an alias for `MiaChainsaw`. Exclude wins when a type appears in both lists.

## Copyable trigger patterns

The coordinates below are illustrative, **not approved placement locations**. Replace them with positions measured in your encounter. Use one example at a time; do not append all of them under the same name.

### Appear when the player enters an area

```csv
Name,Parameter,State,X,Y,Z,Radius,Time,Notes
MyFirstEncounter,spawn,,10,0,20,3,0,Appear on entering the measured sphere
```

This is sufficient for a one-time appearance. No `resume` row is needed. Leaving the sphere does not hide the enemies.

### Activate, suspend at an exit, and resume on return

```csv
Name,Parameter,State,X,Y,Z,Radius,Time,Notes
MyFirstEncounter,spawn,,,,,,0,Enable once a native member is available
,resume,,10,0,20,3,0,Activate on entering the encounter
,suspend,,30,0,20,3,0,Suspend at the exit
```

The empty `Name` cells continue `MyFirstEncounter`. Because this group has a `resume` row, it initially waits for that sphere even though `spawn` has fired. Crossing the exit sphere suspends it. Leaving and re-entering the resume sphere activates it again. Use distinct spheres so suspend and resume do not fire together. Walking out of either sphere is not itself an action.

### Finish permanently at an exit

Add this to the same group only if returning must never reactivate it:

```csv
,despawn,,50,0,20,3,0,Finish at the final exit
```

`despawn` completes the native slots. It cannot be undone with `resume`, and does not resurrect killed enemies. Use `suspend` for a reversible exit.

### Add a story phase

1. Open the [state reference](spawn_group_states.md), choose the campaign section and a documented phase, and copy the entire **State** value.
2. Paste it into the `State` cell of the rule that should depend on that phase. Keep the pipes and exact state spelling.
3. If the same row has a position sphere, the state **and** player position must match together. Do not put the state on a second `spawn` row to express AND; separate rows are independent triggers.
4. Start before that phase, then enter the sphere while it is active. Also test arriving after it and reloading a checkpoint. An exact state tests the controller's current state, not "at or after this milestone".

A loaded controller can retain a state while inactive. In the live chapter-three check, later story controllers already reported `0` before their sections started. **Do not use `0` as proof that a chapter has started.** Use the reference's phase notes and a position condition; State alone does not check the active chapter.

The format is `FsmGameObjectGuid|CoreId|StateName`. The reference supplies all three parts; the GUID is the scene identity of the object that owns the FSM. It is not a Uvar GUID, enemy GUID, or FSM `InstanceGuid`. Short unqualified names are accepted only when exactly one loaded FSM matches on core `0`; names such as `100` or `End` are unsuitable without an owner.

`c03_1_Main` and `c03_1_Main_GarageBattleEnd` from the Uvar list cannot be used here. Boolean expressions, numeric comparisons, and current-main-flow names are not supported. Do not turn `c03_1_Main` into `...|0|c03_1_Main`; qualification cannot create a missing state. If the reference does not cover your milestone, use a position trigger or request a documented entry with the checkpoint and intended before/after behavior. The [verification procedure](spawn_group_states.md#adding-or-verifying-an-entry) explains what evidence an entry needs.

## Column and timing reference

| Column | Meaning |
| --- | --- |
| Name | Group name. Blank continues the previous named group; a repeated name adds conditions to it. |
| Parameter | `spawn`, `despawn`, `suspend`, or `resume`. |
| State | Optional. Paste a qualified FSM state from the reference. Leave empty for position or load-delay triggers. |
| X, Y, Z, Radius | Player world-position trigger. Supply all four with a positive Radius, or leave all four empty. |
| Time | Nonnegative delay in gameplay seconds after the trigger first matches; blank means zero. |
| Notes | Author notes, ignored by the runtime. |

State and position on one row must **both** match. Separate rows are independent triggers. A delay latches when its condition becomes true and continues after leaving the position/state: `Time = 5` does not require remaining inside for five seconds. Pausing, scene loading, and periods without a player stop the controller clock.

Rules are evaluated only after a native member has been discovered. `spawn` without a state or position starts its delay then. Other parameters require a state or position. With no spawn rows, spawning is enabled immediately. With no resume rows, activation is enabled immediately. With both, a spawn and a resume must fire. `spawn` fires once per row; `suspend` and `resume` rearm after their condition stops matching and matches again.

`suspend` preserves native health/limb backup for generator members; static actors are deactivated without resetting health or AI. `despawn` is terminal. Completed/dead slots never resurrect. Due events run by deadline, then spreadsheet row order; terminal despawn wins even when other actions are due in the same update.

Triggers are sampled every 0.1 seconds, while state ownership is resolved every two seconds. Brief states can be missed, and a phase that ended before the group loaded is not remembered. A spawn rule does not continuously enforce its state: leaving the phase does not automatically suspend the group.

## Generate and install your changes

For a spreadsheet test, enable **Debug > Developer Tools > Refresh Spreadsheet Data** in the generation profile (`debug-download-data = true`). It fetches current shared sheets when generating. Otherwise generation reads the CSV snapshot embedded in the build; editing the sheet alone has no effect. Refreshing can change the result for the same seed. Keep the seed, full profile, and data revision with your test notes.

For a local, isolated test, edit the relevant CSV snapshots under `src/Biohazard.BioRand.RE7/_Data/`: `spawn_groups.csv` plus `enemies.csv` or `extra_enemies.csv`. Use `debug-download-data = false` and rebuild when generating. This avoids changing other contributors' shared rows. Copy the full `default-profile.json` to a local `spawn-test.json` and edit its options; do not assume a partial configuration is merged with defaults.

```powershell
dotnet run --project .\src\biorand-re7\biorand-re7.csproj -- generate --seed 35825 --config .\spawn-test.json --output .\out\spawn-test.zip
```

The command requires the usual baseline setup; see the [CLI and build instructions](../../README.md). A `.zip` output is a **Fluffy Mod** archive. A `.pak` output also writes required companion PAKs and runtime files beside it; install all of them, not just the named PAK.

Maintainers can run CLI `update` to replace embedded snapshots from the shared sheets, then rebuild. **It refreshes all mapped data sheets**, not just spawn groups; review every changed CSV before committing. There is no separate on-disk spreadsheet cache to clear.

Close RE7 before replacing installed seed files. Install the **whole generated release**: all game data (PAKs for a Patch installation, or the complete Fluffy archive), Lua modules, `config.json`, and `reframework/data/BioRand7/spawn_groups.json`. Follow [Installing a generated seed](../../README.md#installing-a-generated-seed). Copying only Lua or a manually edited manifest does not disable original native spawn requests. **Reload config** in the BioRand UI reloads installed config/manifest files; it does not download the spreadsheet or regenerate scenes.

### Verify before sharing an encounter

1. Open generated `reframework/data/BioRand7/spawn_groups.json`. Find the group's `name` and confirm its `conditions` and nonempty `members`. An absent group/member is a generation or placement-selection issue; a present group that never activates is a runtime/trigger issue.
2. Enable **BioRand 7 > Debug tools > Verbose logging** in REFramework. Trigger matches are logged as `SpawnGroup MyFirstEncounter: spawn`, `resume`, `suspend`, or `despawn` in `re2_framework_log.txt` beside the game executable. A logged trigger proves the rule fired; native spawning can still fail or wait for an available pool.
3. Approach from outside the sphere, cross it, leave through each exit, and return. Check actual enemies and progression, not just the log. Test nearby floors and loading boundaries when relevant.
4. Test pause, checkpoint reload, room re-entry, and killed enemies. Save-load resets pending delays and trigger history; native spawned/suspended/completed records determine restoration. A killed/completed member must stay completed.
5. Record the seed/profile/build, sheet rows, exact State (if any), location and difficulty, start checkpoint, and results. Distinguish a state that resolves from a complete encounter test. Start with one ordinary Molded; add density and special enemies after the small encounter works.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| `Unable to read data file` for SceneFile | Exact `Areas.Path`, native spelling, `.scn.20` suffix, and no internal folder appended. |
| `Unknown SpawnGroup` | Name defined on the group sheet; placement uses that name; both sheets came from the same generation input. |
| No group/member in manifest | Data refresh/build, extra row Enabled, extra-placement percentage, scene limits, Include/Exclude pool, and preserve/cull tags. |
| Group present, no trigger log | Verbose logging enabled; unpaused; native member loaded; correct Y; State exists and is currently active. |
| `Ambiguous SpawnGroup State` | Replace the short name with the full value from the reference. |
| State never triggers | Do not use a Uvar/chapter label. Check owner scene, exact phase, and validation status. Missing/unloaded FSMs retry silently; no error does not establish a match. |
| Spawn logged but enemies hidden | A required resume rule must also fire. Check later suspend/despawn events and `SpawnGroup request failed` native errors. |
| Spawns early or after leaving a sphere | Pools can load early; use a position trigger. Delays continue after the initial match. Separate spawn rows are alternatives. |
| Cannot resume after exit | Use suspend for reversible exits. Despawn and death/completion are terminal. Leave and re-enter the resume sphere to rearm. |
| Edits have no effect | Regenerate with current data and reinstall complete matching output. UI Reload config cannot apply scene/PAK changes. |
| Manifest version/seed mismatch | Install config, manifest, scripts, and PAKs from the same release; mismatch disables the controller. |

## Supported placements and interactions

Generator-backed groups support `Molded`, `MoldedBlade`, `MoldedQuick`, `MoldedFat`, `FlyingBug`, `JackStalker`, `JackShears`, and `MargeMutated` placements. `InsectHive` and `InsectSwarm` are excluded from new placements for stability; see [the swarm restriction](enemy_spawning.md#swarm-stability-restriction). Existing native insect members remain recognizable, including hive aliases `Em5511`/`Em5512`. Boss types are supported as randomized replacements or extras; their original scripted encounters remain protected by placement rules.

Static groups also support `MiaChainsaw` extra placements and `EvelineElderly` replacements/explicit extras. This covers all 10 definitions currently supported for enemy placement. `JackMutated` is registered for health tuning only (`SupportsRandomEnemyPlacement = false`) and remains excluded. Mia retains the existing extra-placement restriction: she has no generator spawn option and is not in the ordinary replacement pool. Elderly Eveline can be authored as an extra with blank Id and `Include = EvelineElderly`; she is still excluded from the existing configured random-extra pool.

Legacy native IDs and pipe-separated lists in `Id` remain accepted, but canonical Include/Exclude IDs are clearer for new rows.

- `preserve` and `cull` take precedence over group membership. Preserved slots keep their original encounter control; culled slots are removed.
- `aggro` is retained and applied on activation. `nodup` still prevents multiplier copies.
- Randomized group members use the existing configured pool, with the existing Include/Exclude and scene compatibility rules still applied. An incompatible final native slot fails generation.
- Multiplier clones inherit their original group. Slots removed or disabled by the multiplier/scene-limit path are excluded from the final manifest and cannot be reactivated by the controller. Existing scene-limit rules are unchanged.
- Selected extra placements register their actual generated scene and final GUID. Extras skipped by amount/limit/pool selection do not become manifest members.
- Scripted flashbacks, protected native slots, original boss sequences, DLC encounters, and original scripted static actors are not supported. `waypoint` rows fail explicitly; navigation and boss progression need separate runtime work.

Keep a group scoped to one encounter whose pools and actors load together. The runtime restores group activation from the first available native members; it is not a chapter-wide encounter sequencer.

## Maintainer runtime reference

`SpawnGroupModifier` runs after enemy replacement and multiplication. It disables autonomous spawn and respawn conditions for controlled slots, prepares in-place suspension, and disables only their `app.fsm.EnemyGenerate` requests, including references in separate difficulty/level scenes. Static replacements transfer group membership to the new actor and retire the original spawn-info references. Grouped static actors are serialized with Update/Draw disabled. It does not rewrite FSM state UIDs or unrelated progression actions.

Both release formats include `BioRand7/spawn_groups.lua`, `BioRand7/spawn_group_engine.lua`, `BioRand7/spawn_group_static.lua`, and `reframework/data/BioRand7/spawn_groups.json`. The version-2 manifest records the final scene GameObject GUID, member kind, and native `EnemySpawnInfo.MyGUID` or static group-save GUID, plus group conditions, seed, and aggro flag. Membership requires REFramework. Empty manifests overwrite stale previous-seed data; a mismatched manifest seed/version disables the controller and logs an error. Install the complete generated release, since Lua alone cannot disable the scene's original generation requests.

Runtime discovery reads registered `EnemyGeneratorManager` pools once per gameplay second. Static discovery scans native `OtherObjectSave` components at the same interval. FSM resolution retries every two seconds and ignores ambiguous names with a diagnostic. Native requests are asynchronous; pending operations are allowed to finish and unavailable pools are retried at most once per second per member. A native request exception is logged without stopping other members.

Room streaming retains fired triggers in memory. Save-load/new-game hooks reset the controller and discover native spawned/suspended/completed state again. Native active members bypass an already-passed spawn trigger; native suspended members remain suspended until a resume condition fires. Custom pending delays and fired rule history are **not serialized into the save**: delays restart and repeatable conditions can trigger again after loading. Use stable progression states or re-enterable volumes for checkpoint-sensitive encounters. Script reset/config reload also resets transient trigger history.

### Static actor save state

`spawn_group_static.lua` stores waiting/active/suspended/completed state as a two-integer `BRSG` marker/state suffix in native `OtherObjectSaveDataClass.OtherInt`. It preserves the existing array prefix and updates an existing suffix without growing it repeatedly. Elderly Eveline uses her `app.Em3300.Em3300Save`; Mia receives a separate `app.OtherObjectSave` with a deterministic unique GUID and `IsNotSaveBasicData = true`. Mia's original `EnemySave` still owns health, transform, AI, and death data. No existing AI state names, health values, or global save slots are repurposed for group activity.

The adapter hooks the base and Eveline-specific native save/load methods, matches only manifest GUIDs, and restores group activity after streaming or loading. Mia's native death record and the existing static-Mia death guard override group activation. Explosive Eveline records completion before destruction. Pending condition timers still follow the reset rules above. Runtime field/method names were checked against the local RT TDB dump; the save integration needs live gameplay validation.

## Validation and remaining gameplay checks

C# regression tests generate and serialize both replacements and explicit Include/Exclude extras for every currently supported generator-backed enemy. Static tests cover both extra types, replacement membership transfer, unique save records, initial inactivity, and one mixed group containing every supported spawn enemy. They also cover parsing/errors, tag precedence, multiplier membership, scene-limit suppression, extra-placement scene identity, serialized native fields, external generation references, and release manifests. Lua tests execute the production controller and native adapter against strict mocks for trigger conjunction, delays, pause, streaming, restore, simultaneous events, asynchronous requests, retry limits, and completed-slot protection. Static Lua tests additionally cover native save round trips, preserving native array data, earlier-save restoration, health retention, death, nested save hooks, and leaving ungrouped actors untouched.

Live RE7 RT validation on 2026-10-07 ran the production Lua modules against a native Em4100 slot. A qualified current FSM state plus a player-position sphere triggered delayed spawning; the probe observed suspension, resumption, and terminal despawn. Native suspension reported the Self variant, so this does not independently prove completion of the explicit external suspension request. The test harness restored the slot's original unspawned state and was removed. A first Em4000 slot rejected `requestSpawn` with a native null-reference exception in that scene; per-member exception isolation and bounded retries handle this without blocking the group, but that slot's native readiness cause remains unresolved.

The live clock probe established that `via.Application.get_ElapsedSecond()` is seconds per frame; `get_DeltaTime()` is scaled to a 60 Hz frame and must not drive these delays. Further gameplay validation is needed for all Molded variants, an installed generated group encounter, checkpoint reloads, room transitions, explicit external suspension, and progression after encounters. No full playthrough or save/load certification is implied by the focused live cycle.

The 2026-10-08 expansion was validated offline. In-game checks remain for supported special enemies, mixed generator/static groups, initially hidden actor initialization, Mia and Eveline native save round trips, terminal despawn, and room re-entry. Swarms and hives are excluded from new randomized placements. The October 7 Molded probe does not establish these other behaviors. The [state reference](spawn_group_states.md) records the separate October 9 read-only trigger lookup checks; resolving a state does not validate an entire encounter.
