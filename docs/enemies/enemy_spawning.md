 Enemy spawning in re7.exe is centered on three types:

  - app.EnemyGeneratorManager
  - app.EnemyGenerator
  - app.EnemyPool / app.EnemySpawnInfo

  The top-level entry is app.EnemyGeneratorManager::requestSpawn318333 at 0x1403B0070, which quickly gates spawning behind a global FSM bool, then
  calls requestSpawn318334 at 0x140DA7680. That second function validates the EnemySpawnInfo request, rejects already-completed or already-live
  entries, checks the source GameObject is active, then iterates this->generators and calls app.EnemyGenerator::spawn50594 at 0x1417999A0 until one returns a live GameObject.

  Inside a generator, the real work is split between:

  - app.EnemyGenerator::spawn50594 at 0x1417999A0
  - app.EnemyGenerator::spawnImplement50605 at 0x14179BAB0
  - app.EnemyGenerator::getFoundSpawnInfo50604 at 0x141750C10
  - app.EnemyGenerator::doUpdate50587 at 0x141743660

  What it does at a high level:

  - The generator locks itself and records the current requested EnemySpawnInfo.
  - It looks for matching spawn records in its pool by Guid and by UnitAlias.
  - It prefers reusing pooled instances rather than constructing fresh ones. app.EnemyPool::pickupInstanceAndInfo50573 at 0x1416657C0 builds those
    EnemySpawnInfo components into SpawnInfos and ForceSpawnInfos.
  - Normal spawn path: spawnImplement50605 finds a matching pooled unit, writes that unit’s GameObject into info->EnemyInstance, then calls
    app.EnemySpawnInfo::spawnInstance90109 at 0x1414AB3D0.
  - Force-spawn path is mixed into spawnImplement50605: if info->IsForceSpawn is set, it searches force-spawn candidates, can skip some correction/
  - requestOrder90106 at 0x1400C94F0 forwards an order to the spawned enemy if an EnemyOrder exists.
  - reserveAppearAndThinkStateSet90116 at 0x1400CEA90 stores delayed AI appear/think sets for application after spawn.
  - spawnInstance90109 marks the request as spawned, clears respawn wait, binds controllers off the live EnemyInstance (EnemyDamageController,
    EnemyActionController, EnemyStatus, EnemyOrder, MovementController, etc.), activates/configures the object, and applies movement/filter/map
    policy says cancellation counts as completion.

  The main runtime model looks like this:

  1. Pool startup scans level objects and pre-registers enemy instances plus their EnemySpawnInfo records.
  2. A spawn request comes in through EnemyGeneratorManager::requestSpawn.
  3. The manager tries each EnemyGenerator.
  4. A generator matches the request to a pooled spawn slot by GUID/alias.
  5. It binds the pooled GameObject into EnemySpawnInfo, calls spawnInstance, and optionally setupInstance.
  6. If the request is force-spawned or time-limited, extra handling in spawnImplement decides whether to complete, defer, or cancel the request.

  The important conclusion is that RE7 is not primarily “creating enemies from scratch” on each request here. The code I found is mostly a pooled-
  instance activation system driven by EnemySpawnInfo records and EnemyGenerator selection logic.

## DLC enemy component shapes

Main-game generator scenes normally use this hierarchy:

- `app.EnemyGenerator`
- `app.EnemyPool`
- `app.EnemySpawnInfo`
- `app.EnemySpawnInfoOptionEm####`

Not a Hero (CH8) scenes use their own generator and pool subclasses:

- `app.CH8EnemyGenerator`
- `app.CH8EnemyPool`
- `app.CH8EnemySpawnInfo`
- `app.CH8EnemySpawnInfoOptionEm####`

End of Zoe (CH9) enemy spawn-info objects use:

- `app.CH9EnemySpawnInfo`
- `app.CH9EnemySpawnInfoOptionEm####`

No `app.CH9EnemyGenerator` or `app.CH9EnemyPool` type was found in the current TDB, but CH8 definitely has both. For CH8 enemies in main-game generator scenes, replacing only the enemy-specific option component is not enough. The randomized generator should also be promoted to `app.CH8EnemyGenerator` and its child pool to `app.CH8EnemyPool`, while the individual spawn info should become `app.CH8EnemySpawnInfo`.

`app.EnemySpawnInfoOptionDLC` exists and `app.EnemySpawnInfo` exposes DLC-option-like state, but real DLC spawn infos do not universally include it. Prefer copying the real imported DLC spawn-info object shape for the target alias and copying only shared campaign fields from the original main-game spawn info.

Live REFramework MCP singleton probing confirmed `app.EnemyGeneratorManager` is available in the running game. Use that runtime object, plus `app.DLCContentSceneManager`, `via.ResourceManager`, and `via.SceneManager`, when serialized DLC scene output looks correct but enemies or assets still do not appear.

Important CH8 alias quirks:

- `Em4210` is Fat Headless Molded. Its `UnitAlias` is `Em4210`, but its component/option stack is based on `CH8Em4200`.
- `Em4600` is Fumer. Its `UnitAlias` is `Em4600`, but its component/option stack is based on `CH8Em4000`.
- `Em4460` is Mama Mold. It has a normal generator-style source and also appears in a wrapper-style object with nested `Em4450` spawn info.
- `Em4500` is Mutated Lucas, the Not a Hero final boss. Treat it as a special-case/boss enemy, not a safe baseline integration target.

## Scene limit mapping

`app.fsm.EnemyGenerate::start357603` resolves its target `EnemySpawnInfo` by GUID first. It can fall back to
`app.ObjectManager::findObjectInContainer170087(containerName, objectName)`, but the base-game molded/hard spawn scenes inspected for this
work had empty `GameObjContainer` names and direct `SpawnInfo` GUID references.

IDA also showed that `via.SceneManager::loadScene267475` takes a path string, hands it to the resource manager, and the resource manager hashes
the path internally for lookup/caching. `EnemyGenerate` itself does not store that hash or a full path; it stores the target `SpawnInfo` GUID.
The useful static mapping is therefore:

1. collect enabled `EnemyGenerate.SpawnInfo` GUID references from the General scene being limited,
2. resolve those GUIDs through the vanilla `enemies.csv` spawn-info data,
3. disable surplus `EnemyGenerate` actions in the General scene only when the multiplier is already reducing the vanilla count.

This avoids a manual vanilla-scene-file column. The pooled `EnemySpawnInfo` records can still live in separate files such as
`natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20`; the cap acts on the General scene's requests that point at those records.
For neutral or upward multipliers, scene limits cap added vanilla enemies but do not delete the baseline vanilla requests.

## Enemy placement spreadsheet directives

`Enemy Placements` / `_Data/enemies.csv` uses `Tags`, `Include`, and `Exclude`.
The embedded snapshot and downloaded sheet go through the same `EnemyPlacementService`.
Rows are matched by normalized scene path and GUID; generation actions in another
scene resolve their spawn-info GUID back to the placement rule.

- `preserve` keeps the original placement out of replacement and count changes.
  Probabilistic forced targeting also leaves it alone. Explicit `aggro` can still
  target the player while keeping the original enemy type.
- `aggro` sets `EnemySpawnInfo.IsPlayerTargetingAtStart` and supported spawn options'
  `IsForceTargetingToPlayer` after replacement. It works with enemy randomization
  disabled and survives multiplication. It requires an `IsSpawnInfo` row; static
  actors have no generic supported targeting path.
- `nodup` excludes the spawn from duplication. It still permits replacement and
  removal when the enemy multiplier reduces the count.
- `cull` removes the selected enemy even when randomization is disabled, and
  disables `app.fsm.EnemyGenerate` actions referencing its spawn-info GUID across
  campaign scenes. Shared pooled instances remain available to other spawn slots.
  For mesh rows under an `app.EnemySave` actor, culling removes that actor root
  rather than leaving an invisible active enemy. Do not apply this to a required
  boss/story actor without checking the progression FSM.

Tags are whitespace-separated and case-insensitive. Legacy `prefab` and `exclude`
tags are accepted as `preserve`. Unknown tags and contradictory `cull preserve` /
`cull aggro` combinations fail with the placement GUID and scene in the error.
Flashback scenes remain protected; explicit `aggro` or `cull` there is rejected.

`Include` and `Exclude` are whitespace-separated `EnemyDefinitions.Id` patterns,
case-insensitive, with `*` and `?` wildcards. They match definition IDs such as
`MoldedBlade`, not `Em4000` aliases: normal and blade Molded share that engine alias.
Blank Include admits the configured pool; Exclude wins over Include. Filters
intersect configured ratios, balance restrictions, and native encounter safeguards.
If the area variety pool has no allowed candidate, selection retries the full
compatible configured pool. If that is also empty, the original enemy stays.
Different slot restrictions can therefore exceed an area's requested variety.

The curated snapshot preserves templates, pooled actors, bosses, flashbacks, barn
and pit fights, and special crawler appearances. Ordinary Molded slots allow normal,
blade, and four-legged Molded; existing fat slots additionally allow fat Molded.
Old House insect spawns admit only insects. No slot is culled by default. DataGen
emits the same schema and conservative defaults; running the enemies generator
rebuilds its output, so refresh from the sheet to retain subsequent manual edits.

Regression tests cover the CSV rules, serialized replacements and targeting,
cross-scene culling, and multiplier restrictions. In-game validation is still
needed for progression after authoring new culls and for unusual replacement pools.
