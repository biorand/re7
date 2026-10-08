# SpawnGroups

RE7 uses the RE9 spreadsheet's `Name, Parameter, State, X, Y, Z, Radius, Time, Notes` layout and continuation rows. The runtime below is RE7-specific; RE9 source code was not available to establish identical engine behavior.

The [RE7 SpawnGroups tab](https://docs.google.com/spreadsheets/d/1YNdX9LWrhh6KDKd8Mx7JpTCMq8XY8u6BfX20YYNx9jk/edit?gid=154763280#gid=154763280) is downloaded as `spawn_groups.csv` by the usual dynamic-data update. The embedded default contains the header only. This feature is opt-in: author groups and assign placements before generating a new seed.

## Authoring

Put the same group name in `SpawnGroup` on **Enemy Placements** (column T) or **Extra Enemy Placements** (column O). Names are trimmed and matched without case sensitivity. A placement belongs to at most one group. Unknown groups and conflicting memberships fail generation.

| Column | Meaning |
| --- | --- |
| Name | Group name. Blank continues the preceding named group; a repeated name adds conditions to that group. |
| Parameter | `spawn`, `despawn`, `suspend`, or `resume`. |
| State | FSM state name or full state path. An unqualified name must resolve to exactly one loaded FSM, on core 0. Prefer `FsmGameObjectGuid\|CoreId\|StateName` to identify its owner and core explicitly. The GUID is the FSM owner's scene GameObject GUID, not an enemy ID or FSM InstanceGuid. |
| X, Y, Z, Radius | Player position trigger in world coordinates. Supply all four, with a positive radius. Uses spherical 3D distance, including height. |
| Time | Nonnegative delay in gameplay seconds after the trigger first matches; blank means zero. |
| Notes | Author notes, ignored by the runtime. |

State and position on one row must **both** match. Separate rows are independent triggers. A delay latches when its condition becomes true and continues after leaving the position/state. Pausing, scene loading, and periods without a player stop the controller clock. Triggers are sampled every 0.1 seconds, so short-lived FSM states may be missed.

`spawn` without a state or position starts its delay when a member is first discovered in a loaded native pool. Other parameters require a state or position. `spawn` enables the group; when resume rows exist, a resume trigger must also have fired before enemies become active. With no spawn rows, spawning is enabled immediately. With no resume rows, activation is enabled immediately.

`suspend` hides/stops spawned members through the native suspension request, retaining their native health/limb backup. `resume` enables activation again, without resetting that backup. Both rearm after the condition stops matching and is entered again. `spawn` fires once per row. `despawn` is terminal: it uses the native kill/completion path and cannot be reversed by a later resume or spawn. Completed/dead native slots are never resurrected.

Due events run by deadline and then spreadsheet row order. A terminal despawn wins even when other actions become due in the same update.

Example only; replace the coordinates with a validated encounter location before using it:

```csv
Name,Parameter,State,X,Y,Z,Radius,Time,Notes
ExampleEncounter,spawn,,,,,,0,Enable when the pool loads
,resume,,10,0,20,6,0,Activate when the player enters this sphere
,suspend,,30,0,20,6,0,Suspend at a second location
,despawn,,50,0,20,6,2,Finish two seconds after entering the exit sphere
```

## Supported placements and interactions

Generator-backed groups support `Molded`, `MoldedBlade`, `MoldedQuick`, `MoldedFat`, `FlyingBug`, `InsectHive`, `InsectSwarm`, `JackStalker`, `JackShears`, `JackMutated`, and `MargeMutated`. Native hive aliases `Em5511`/`Em5512` share the `InsectHive` stack. Boss types are supported as randomized replacements or extras; their original scripted encounters remain protected by placement rules. Extra Enemy Placements uses the same canonical Include/Exclude IDs. Leave Id blank for an explicit Include pool, or use `random` to filter the configured pool. Legacy native IDs and pipe-separated lists remain accepted.

- `preserve` and `cull` take precedence over group membership. Preserved slots keep their original encounter control; culled slots are removed.
- `aggro` is retained and applied on activation. `nodup` still prevents multiplier copies.
- Randomized group members use the configured generator-backed pool, with the existing Include/Exclude and scene compatibility rules still applied. An incompatible final native slot fails generation.
- Multiplier clones inherit their original group. Slots removed or disabled by the multiplier/scene-limit path are excluded from the final manifest and cannot be reactivated by the controller. Existing scene-limit rules are unchanged.
- Selected extra placements register their actual generated scene and final GUID. Extras skipped by amount/limit/pool selection do not become manifest members.
- Scripted flashbacks, protected native slots, original boss sequences, DLC encounters, and direct/static actor placements are not supported. `waypoint` rows fail explicitly; navigation and boss progression need separate runtime work.

Keep a group scoped to one encounter whose pools load together. The runtime restores group activation from the first available native members; it is not a chapter-wide encounter sequencer.

## Generation and runtime

`SpawnGroupModifier` runs after enemy replacement and multiplication. It disables autonomous spawn and respawn conditions for controlled slots, prepares in-place suspension, and disables only their `app.fsm.EnemyGenerate` requests, including references in separate difficulty/level scenes. It does not rewrite FSM state UIDs or unrelated progression actions.

Both release formats include `BioRand7/spawn_groups.lua`, `BioRand7/spawn_group_engine.lua`, and `reframework/data/BioRand7/spawn_groups.json`. The manifest records the final scene GameObject GUID and native `EnemySpawnInfo.MyGUID`, group conditions, seed, and aggro flag. Membership requires REFramework. Empty manifests overwrite stale previous-seed data; a mismatched manifest seed/version disables the controller and logs an error. Install the complete generated release, since Lua alone cannot disable the scene's original generation requests.

Runtime discovery reads registered `EnemyGeneratorManager` pools once per gameplay second. FSM resolution retries every two seconds and ignores ambiguous names with a diagnostic. Native requests are asynchronous; pending operations are allowed to finish and unavailable pools are retried at most once per second per member. A native request exception is logged without stopping other members.

Room streaming retains fired triggers in memory. Save-load/new-game hooks reset the controller and discover native spawned/suspended/completed state again. Native active members bypass an already-passed spawn trigger; native suspended members remain suspended until a resume condition fires. Custom pending delays and fired rule history are **not serialized into the save**: delays restart and repeatable conditions can trigger again after loading. Use stable progression states or re-enterable volumes for checkpoint-sensitive encounters. Script reset/config reload also resets transient trigger history.

## Validation and remaining gameplay checks

C# regression tests generate and serialize both replacements and explicit Include/Exclude extras for every registered generator-backed enemy. They also cover parsing/errors, tag precedence, multiplier membership, scene-limit suppression, extra-placement scene identity, serialized native fields, external generation references, and release manifests. Lua tests execute the production controller and native adapter against strict mocks for trigger conjunction, delays, pause, streaming, restore, simultaneous events, asynchronous requests, retry limits, and completed-slot protection.

Live RE7 RT validation on 2026-10-07 ran the production Lua modules against a native Em4100 slot. A qualified current FSM state plus a player-position sphere triggered delayed spawning; the probe observed suspension, resumption, and terminal despawn. Native suspension reported the Self variant, so this does not independently prove completion of the explicit external suspension request. The test harness restored the slot's original unspawned state and was removed. A first Em4000 slot rejected `requestSpawn` with a native null-reference exception in that scene; per-member exception isolation and bounded retries handle this without blocking the group, but that slot's native readiness cause remains unresolved.

The live clock probe established that `via.Application.get_ElapsedSecond()` is seconds per frame; `get_DeltaTime()` is scaled to a 60 Hz frame and must not drive these delays. Further gameplay validation is needed for all Molded variants, an installed generated group encounter, checkpoint reloads, room transitions, explicit external suspension, and progression after encounters. No full playthrough or save/load certification is implied by the focused live cycle.
