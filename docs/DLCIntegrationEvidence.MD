# CH8 and CH9 integration reverse-engineering evidence

Status: Confirmed static evidence with runtime-correlated conclusions

Last updated: 2026-08-25

## Scope and evidence rules

This appendix records concrete IDA MCP evidence used by [DLCIntegrationPlan.MD](DLCIntegrationPlan.MD). It distinguishes three levels:

- **Confirmed static:** directly visible in decompiled native control flow.
- **Confirmed runtime:** observed in a live source-DLC or imported campaign instance and recorded in the existing local analysis notes.
- **Inference:** an implementation conclusion that follows from the static and runtime evidence but still needs a controlled game test.

No raw virtual address in this document is stable across executable builds.

The runtime observations refer to the earlier DLC prototype and local `.analysis/dlc-enemy-integration-findings.md` notes, which are not included in a fresh checkout. They do not certify campaign support on `master`. The addresses and signatures below remain evidence for the analyzed executable, not current production hooks.

## Analyzed executable

| Field | Value |
| --- | --- |
| Module | `re7.exe` |
| Product/file version | `1.0.0.7` |
| Image base | `0x140000000` |
| File length | `156313064` bytes |
| SHA-256 | `D550260029B51A580E206B9A12A4EB7F4A1B4DD230BBA5DEFCCA81DA40597321` |
| IDA auto-analysis | Complete |
| Hex-Rays | Available |

Production hooks must verify the executable hash or validate a unique instruction signature and its surrounding semantics. Unknown builds must fail closed.

## CH8 action construction

### The failure is before the derived action-insertion block

`app.CH8Em4400ActionController::doAwake149958` at `0x1418DDB00` has this confirmed shape:

1. Calls `app.CH8MoldedActionController::doAwake144706` at `0x141E35C20`.
2. Checks the managed exception slot and returns if the base call failed.
3. Reads `MyCommandActionContainer`.
4. Constructs native Em4400 command actions and inserts them into the container dictionary.
5. Binds status, think, navigation, player, and other object references.

There is no explicit chapter-number check before the action insertion. The earlier runtime observation that a campaign call to Em4400 `doAwake()` left the container empty is explained by failure inside the base call.

### CH8 base controllers require CH8GameRankManager

`app.CH8EnemyActionController::doAwake150859` at `0x1410EF950`:

1. Calls the ordinary `app.EnemyActionController::doAwake166844`.
2. Returns early when no initialized `EnemyRankManager` is present.
3. Registers four rank-change handlers.
4. Reads the `app.CH8GameRankManager` singleton.
5. Takes a managed null/error path when that singleton is absent.
6. Otherwise reads the CH8 game rank and calls `EnemyRankManager.setRank(rank, false, true)`.

`app.CH8MoldedActionController::doAwake144706` contains the same CH8 game-rank dependency after its ordinary molded-base setup and rank-handler registration.

The derived controller checks the managed exception slot immediately after calling either base. Consequently, a missing `CH8GameRankManager` prevents native command action creation.

### Known family coverage

IDA confirmed the first native call in these derived awake methods:

| Runtime family | Derived awake | First base awake | Affected aliases |
| --- | --- | --- | --- |
| `CH8Em4000` | `doAwake146854` | `CH8MoldedActionController.doAwake144706` | Em4600/Fumer stack |
| `CH8Em4200` | `doAwake148875` | `CH8MoldedActionController.doAwake144706` | Em4210/Fat Headless stack |
| `CH8Em4400` | `doAwake149958` | `CH8MoldedActionController.doAwake144706` | Em4400/Mama Mold |
| `CH8Em4450` | `doAwake151433` | `CH8EnemyActionController.doAwake150859` | Em4450/Little Crawler |

This is a component-family dependency, not an Em4400-only defect.

### Campaign rank is applied later by the generic setup path

`app.EnemySpawnInfo::setupInstance90108` at `0x1414A8E40` does not use `CH8GameRankManager`. At `0x1414A9102` it calls ordinary `app.GameManager::getGameRank172914`, then calls `app.EnemyRankManager::setRank49255` at `0x1414A913D`.

Inference: a campaign-only hook can preserve rank semantics by bypassing the earlier CH8 singleton synchronization or substituting the ordinary campaign rank. It should not skip the entire CH8 base awake method, because that method also installs handlers and binds components.

### Version-specific signatures

IDA generated unique wildcard signatures for the two rank-sync ranges in this executable:

```text
CH8EnemyActionController, 0x1410EFD1D-0x1410EFD6F
48 8B 9D ? ? ? ? 48 83 78 ? ? 75 ? 48 8B 15 ? ? ? ? 48 8B CF 48 85 D2 74 ? E8 ? ? ? ? 48 8B 4F ? 48 83 79 ? ? 75 ? 48 8B CF 48 85 DB 75 ? 45 33 C0 41 8D 50 ? E8 ? ? ? ? EB ? 45 33 C9 C6 44 24 ? ? 44 8B C0 48 8B D3 E8

CH8MoldedActionController, 0x141E35FCD-0x141E3602F
48 8B 9F ? ? ? ? 48 83 78 ? ? 0F 85 ? ? ? ? 48 8B 15 ? ? ? ? 48 8B CE 48 85 D2 0F 84 ? ? ? ? E8 ? ? ? ? 48 8B 4E ? 48 83 79 ? ? 0F 85 ? ? ? ? 48 8B CE 48 85 DB 0F 84 ? ? ? ? 45 33 C9 C6 44 24 ? ? 44 8B C0 48 8B D3 E8 ? ? ? ? 48 8B 46 ? 48 83 78 ? ? 0F 85
```

These signatures are locators for a future controlled experiment, not sufficient validation by themselves.

## Native command registration

`app.EnemyActionController::doStart166846` at `0x140C60430` calls `app.CommandActionController::regist45234` at `0x140C60471` with `MyCommandActionContainer` before continuing the remaining start setup.

`app.CommandActionController::regist45234` at `0x14229FDB0`:

1. Obtains the container's enumerable interface.
2. Enumerates every command action.
3. Calls each action's virtual setup method with the controller owner and command controller.
4. Appends the initialized action to `ActionList`.

Confirmed runtime correlation: the earlier Em4000 fallback produced an `ActionList` with 38 entries whose owner, action-controller, status, think, motion, animation, and command references were still null. Direct insertion into `ActionList` had skipped native setup.

Implementation consequence: restore native container construction first, then allow native registration. Do not treat nonzero list count as readiness, and do not populate `ActionList` directly.

## Generator and spawn pipeline

### CH8 generator defaults

`app.CH8EnemyGenerator::.ctor323011` at `0x1401066A0`:

- Initializes the inherited alias and lock object.
- Sets inherited `DoomsBehavior.IsEnablePause = true`.

This confirms the source of the paused CH8 generator observed in the campaign prototype. A campaign sidecar must deliberately normalize pause and update state.

`app.CH8EnemyPool::.ctor323010` at `0x14052EA00` is only a thunk to `app.EnemyPool::.ctor50580`. No CH8-specific constructor behavior was found in the pool.

### CH9 generator shape

IDA function queries returned CH8 generator and pool constructors, but no functions named for `CH9EnemyGenerator` or `CH9EnemyPool`.

`app.CH9EnemySpawnInfo::.ctor325718` at `0x14032F790` only:

1. Writes one integer default value, `11`.
2. Calls `app.EnemySpawnInfo::.ctor90120`.

Its only other named method is the direct `KillAllEnemyMissionNo` getter at `0x14032EFF0`.

Inference: base generator/pool plus CH9 spawn-info remains the correct initial implementation model. This still requires runtime proof.

### Normal pooled spawn is two-stage

The following flow is confirmed in native code:

```text
EnemyGenerator.spawnImplement
  -> EnemySpawnInfo.spawnInstance
       IsSpawned = true
       bind live controllers
       activate pooled object
       RequestedOperation = Setup
       isCompletedOperation = false
  -> return pooled instance

EnemyGenerator update closure
  -> switch RequestedOperation
  -> EnemyGenerator.setupImplement
  -> EnemySpawnInfo.setupInstance
       apply think/appear state
       apply campaign rank and health
       assign target
       warp transform
       IsAlive = true
       RequestedOperation = None
       isCompletedOperation = true
       invoke completion handlers
```

Supporting addresses:

| Function/effect | Address |
| --- | --- |
| Pooled `spawnInstance()` call | `0x14179BE65` |
| `RequestedOperation = Setup` | `0x1414ABB90` |
| `isCompletedOperation = false` | `0x1414ABBAF` |
| Generator operation-update closure | `0x141F2F390` |
| Closure dispatch to `setupImplement()` | `0x141F2F5AD` |
| `setupImplement50603` | `0x141798750` |
| `RequestedOperation = None` | `0x1414A9C5C` |
| `isCompletedOperation = true` | `0x1414A9C74` |

The immediate `setupInstance()` call inside `spawnImplement50605` is limited to the force-spawn branch guarded by `IsForceSpawn && CanSpawnAndSetupForForceSpawn` at `0x14179C361-0x14179C381`.

Implementation consequence: a generated scene must prove that the generator update path is active. Reaching `spawnInstance()` is not a successful spawn.

## CH8 need-area queue

`app.CH8EnemySpawnInfo::requestSpawnWithNeedArea189521` at `0x141492E10`:

- Returns when an enemy instance already exists.
- Returns when the spawn info is already completed.
- Requires the spawn-info GameObject's `UpdateSelf` property.
- Locks its private lock object.
- Adds `Operation.Spawn` to `requestOptionWithNeedAreaList` only if not already present.

`app.CH8EnemySpawnInfo::update189520` at `0x1405F5EB0` calls its normal update path and then invokes `requestOperationWithNeedArea189526`.

`requestOperationWithNeedArea189526` at `0x141491E80`:

- Iterates queued operations.
- Uses `CH8AIWorldBlackBoard.EnemyDiraySpawn.requestSpawn` for direct-area requests.
- Routes spawn, kill, suspend, resume, reappearance, and generic operations through `EnemyGeneratorManager`.
- Clears the queue and can requeue Spawn when a direct-area request has not yet been accepted.

Implementation consequence: `app.fsm.CH8EnemyGenerate` introduces dependencies on CH8 spawn-info updates, CH8 area state, and the CH8 AI blackboard. Campaign imports must retain `app.fsm.EnemyGenerate`.

## CH9 awake-path comparison

`app.CH9Em7800ActionController::doAwake159769` and `app.CH9Em7900ActionController::doAwake160547` both call `app.CH9MoldedActionController::doAwake157771`, then perform their native action insertions.

The CH9 molded base calls `app.CH9EnemyActionController::doAwake80243` at `0x141D15D50`. Unlike the CH8 bases, this function has no game-rank or mode-singleton reference. It gathers two components from its own enemy GameObject:

- `app.CH9EnemyAttachObjectManager`
- `app.CH9EnemyResistController`

The Em7800 and Em7900 awake-method call references likewise contain no player or mode manager.

Inference: these two families are better first CH9 cross-mode candidates than the examined CH8 families. This conclusion applies only to awake/action construction; think, navigation, grapple, damage, target, and cleanup behavior still require source-mode and campaign comparison.

## Item construction and interaction

### Generic inventory construction

`app.ItemManager::createItemInstance319138` at `0x142146680`:

1. Calls `findItemDataInternal(itemDataID)`.
2. Returns null when no matching `ItemData` exists.
3. Returns null when `ItemData.ItemPrefab` is null.
4. Instantiates `ItemPrefab` at the owner transform when an owner exists.
5. Parents the new GameObject below the owner transform.

`app.Item::notifyInsertInventory138632` at `0x140C82870` enumerates all `InsertInventoryHandler` delegates and invokes each with the owner inventory. A mode-specific callback on the real inventory prefab therefore executes even when the pickup, messages, and item-box display are correct.

### Scene interaction has its own item ID

`app.fsm.ItemAddTest::start357511` at `0x14045C1B0` branches on its `_Multiple` field:

- Multiple mode calls `canAddMultiple()` and uses the action's list data.
- Single mode calls `isCanAddInventory(_ItemDataID)`.

It does not read an adjacent `app.Item.ItemDataID`. Both the component and FSM action must be updated when a static interaction uses this action.

## DLC-specific item dependencies

### CH9 passive skills and effigies

`app.CH9PassiveSkillItem::doAwake113600` at `0x14153DE70` registers its own insert callback with the contained `app.Item` and derives attack/guard flags from `CH9PlayerPassiveSkill` userdata.

`app.CH9PassiveSkillItem::onInsertInventory113604` at `0x1415621D0`:

1. Gets the inventory owner's GameObject.
2. Resolves and validates `app.ICH9PlayerOrder`.
3. Returns without applying the passive when that interface is absent.
4. Registers attack and guard amulets through separate interface calls.
5. Registers the referenced `CH9PlayerPassiveSkill` through another interface call.

Implementation consequence: Ethan cannot consume the raw CH9 passive callback. Effigy semantics must be translated to the campaign passive system or handled by a narrow insertion bridge.

### Joe's gauntlet

`app.CH9Weapon1600::onInsertInventory328503` at `0x140556DD0` resolves `app.CH9PlayerEquipManager` from the inventory owner, stores it as `OwnerEquipManager`, and calls `registerKnuckleWeapon328504`.

`registerKnuckleWeapon328504` at `0x140D9A120` calls `CH9PlayerEquipManager.registerKnuckleWeapon` only when the manager is alive and the weapon is not already registered. `unregisterKnuckleWeapon328505` at `0x140DC5400` mirrors this path.

Implementation consequence: the gauntlet is coupled to Joe's equip manager before input, animation, mesh switching, or damage is considered. It is a CH9 player-system port, not a data-only melee weapon.

### CH8 and CH9 remedies

`app.CH8WeaponRemedyAppend::use324610` at `0x140AA29F0`:

- Requires a CH8-compatible player-order path from the user.
- Resolves player status and invokes recovery.
- Notifies the CH8 order path of remedy use.
- Updates ordinary healing-use save statistics when available.
- Adds the configured `useRemedies` score when `CH8GameRankManager` is present.

`app.CH9WeaponRemedyAppend::use328562` at `0x1414BDDC0` similarly requires a CH9-specific player-order interface, resolves user status, performs recovery, and notifies the CH9 order path.

Both return failure before healing when the user lacks the expected mode-specific interface.

Implementation consequence: copy the DLC healing values into campaign-native remedy behavior. Do not reuse the raw CH8/CH9 append component for Ethan. CH9 `NumaItem013`, whose source inventory prefab uses the base remedy append, remains the best first item-certification candidate.

## Revised implementation priorities

The IDA evidence changes the next experiments as follows:

1. Test a hash/signature-gated CH8 rank-sync bypass or campaign-rank substitution.
2. After the hook, require native container construction and native `doStart()` registration before any managed fallback.
3. Normalize CH8 generator pause/update state because the constructor explicitly opts into pause handling.
4. Keep base campaign enemy-generate FSM actions; do not enter the CH8 need-area queue.
5. Prefer Em7800 or Em7900 for the first CH9 awake-path experiment, subject to the rest of the runtime score.
6. Keep raw CH9 passives, gauntlets, and CH8/CH9 remedy append components out of campaign item pools.
7. Implement campaign-native semantic adapters for healing and passives.

These priorities remain hypotheses until the relevant runtime certification gates in [DLCIntegrationPlan.MD](DLCIntegrationPlan.MD) pass.
