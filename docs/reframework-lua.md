# REFramework Lua validation

The entrypoint is `_Data/reframework/autorun/BioRand7.lua`; its feature modules live in the adjacent `BioRand7/` directory. `RandomizerOutput` embeds the same scripts into both patch and Fluffy ZIPs. Configuration is read from `reframework/data/BioRand7/config.json`.

## Fields and methods

Generated REFramework.NET C# interfaces expose some raw TDB fields as properties with `[Method]` accessors. Those accessors do **not** prove that `get_Name` or `set_Name` exists in the game. Check the raw `fields` and `methods` sections in `dumps/il2cpp_dump.json`, or query the live TDB schema, before translating them to Lua.

Examples verified during the conversion review:

| Type and member | Lua access |
| --- | --- |
| `app.ObjectManager.PlayerObj`, `ManagedObjects` | `get_field` |
| `app.GameManager.GameDifficulty` | `get_field` |
| `app.PlayerMotionController.ReloadSpeedRate`, `CurrentWeaponID` | `get_field` / `set_field` |
| `app.PassiveSkillItem.Item`, `PassiveSkill`, `PlayerOrder` | `get_field` / `set_field` |
| `app.EnemyActionController.SpawnerGuid`, `ActualUsingGuid` | `get_field` |
| `via.physics.ContactPoint.Position`, `Normal` | `get_field` |
| `via.GameObject.Name`, `Transform`, `Valid` | Actual `get_` methods |
| `app.GameFlowFsmManager.CurrentMainGameFlow` | Actual `get_CurrentMainGameFlow` method |

Use explicit, verified accesses rather than runtime field/method guessing. Nil checks for missing players, components, and unloaded scenes remain necessary. A Lua expression like `value == nil and nil or value:call(...)` is not a safe null-conditional operation: its `or` branch still executes.

Check return types in the RT dump (`reframework/il2cpp_dump_rt.json.gz`) as well: `ObjectManager.getEnemyID` returns `System.Nullable<app.EnemyID>` and must be unwrapped. For identity rotations use `Quaternion.identity()`; REF's [quaternion constructor](https://cursey.github.io/reframework-book/api/types/Quaternion.html) takes `(w, x, y, z)`.

Public references: [managed object field and method access](https://cursey.github.io/reframework-book/api/types/REManagedObject.html), [hook arguments and returns](https://cursey.github.io/reframework-book/api/sdk.html), [hooking shared getter/setter implementations](https://cursey.github.io/reframework-book/api/general/best-practices.html), and [available ImGui bindings](https://cursey.github.io/reframework-book/api/imgui.html).

## Offline tests

Enemy drop and Em3300 streams use a stable spawner/save GUID (or scene/name fallback), never a memory address or a session respawn counter. The same spawned enemy therefore keeps its roll after loading a save. This changes the old address-dependent runtime rolls; it does not change the seeded System.Random implementation.

Use `Game:guid_string` for `System.Guid` fields. In the live RT build, calling `ToString()` on an unboxed GUID through Lua returned unrelated receiver bytes. Formatting the TDB fields `mData1`, `mData2`, `mData3`, and `mData4_0` through `mData4_7` matches the engine's actual GUID and avoids shared or process-dependent identities.

`SaveDataManager.newGameInit()` and `loadLevelUsingLoadData()` reset transient feature state. Room streaming does not. Static Mia suppression is written into native records through `Em2000Order.saveData(app.EnemyStatus.EnemySaveDataClass)` using `Health`, `IsUpdate`, and `IsDraw`, and restored through the corresponding `loadData` method. Hook the save target rather than relying on the `EnemySave` accessors. These signatures and fields were verified against the live RT TDB; live calls confirmed the stable GUID and patched death record. The Lua suite covers an earlier save with a living enemy, a saved death, and a new game. A complete native checkpoint/slot persistence check remains unverified: direct test calls did not reliably execute the full save flow, and direct chest-interaction calls crashed the test session. Validate these through normal gameplay controls before signing off persistence.

Run from the repository root:

```text
lua tests/lua/run.lua
```

These tests execute the real Lua modules and hook callbacks against small, strict REFramework mocks. They cover Em3300 proximity/countdown/bomb/despawn behavior, null entries in object lists, inventory allocation, Birthday skills, Madhouse saves, reload speed, knee-down, drop placement, random-event restoration, overlay drawing, and the original seeded C# random streams. They run in CI with Lua 5.4, including when no baseline PAK is available.

The .NET archive test also checks that every script in both release formats exactly matches its embedded source. Neither test harness can prove native hook execution, prefab availability, physics, or scene lifetime behavior inside RE7.

## In-game checks

Use a newly generated release archive containing the corrected scripts. Existing downloaded archives retain their old embedded versions. Remove any old BioRand managed plugin DLL left by a pre-Lua installation before testing, so both implementations do not run together.

1. Load a seed with explosive elderly Eveline enabled. A marked `Em3300_Static` within five metres should start a three-to-eight-second countdown, request one explosion, then despawn after 0.25 seconds. Ordinary unmarked vanilla Evelines must remain unaffected.
2. Check ScriptRunner and the REFramework log after loading, opening inventory/combine menus, saving on Madhouse without tapes, picking up a Birthday skill, and reloading a modified weapon.
3. Use BioRand's event controls to exercise each effect, including its overlay and expiry. Change scene or reload a save while an effect is active; cleanup should not repeatedly alter player stats or block future events.
4. Kill ordinary enemies and a hive with drops enabled. Check one drop per death, ground placement, hive wall clearance, and static Mia remaining dead after reactivation attempts.

These gameplay checks still require in-game validation. If an explosion cannot create a bomb or fallback effect, the script records a diagnostic instead of silently discarding the failure.

## Runtime performance

The September 2026 MCP investigation found 936 valid managed object entries with no marked Em3300 in the current scene. The original Lua conversion still traversed every entry every frame: at least 3,767 reflected method calls per frame for Eveline detection alone. Enemy random events performed another full traversal with component lookups and target sorting. This preserved the C# algorithm but not its performance; see REF's [API benchmarks](https://cursey.github.io/reframework-book/api_cs/general/benchmarks.html) and [performance guidance](https://cursey.github.io/reframework-book/api/general/best-practices.html).

`object_cache.lua` now spreads discovery across frames, with at most 64 entries per update and a 0.5-second pause between complete sweeps. It scans every managed group, including static enemies outside the Enemy group. Matching objects become available during the sweep; absent objects are pruned only after completion. A manager or collection replacement invalidates the cache immediately. Newly loaded static objects can take one sweep plus the pause to discover; registered Em3300 think actions also seed the cache directly.

Only discovered Evelines receive per-frame proximity/countdown updates. Countdown duration and the 0.25-second despawn delay are unchanged. Enemy-event discovery runs only during enemy events; distance/radius ranking refreshes every 0.25 seconds over cached controllers, while selected objects still receive effects every frame. Destroyed targets are checked before use, and restoration retains all touched objects, including targets no longer nearest to the player.

RE7's TDB and live lists confirm `List<T>.mItems` and `mSize`. Iteration reads those cached field definitions and indexes the backing `SystemArray`, respecting the list count rather than array capacity. Component runtime types and non-virtual GameObject method definitions are cached; the polymorphic enemy-ID getter remains dispatched through its action instance.

Lua regression tests enforce discovery budgets, scan cooldowns, null entries, shrinking lists, scene invalidation, deterministic distance ties, moving/destroyed targets, and restoration. A 936-object, 60-frame idle-scene test now performs 5,616 reflected object calls (peak 192/frame), versus 7,488 after the first performance pass and at least 224,640 before optimization. This is a call-count regression test, not a measurement of in-game frame time.

For an in-game comparison, keep the same save, camera position, graphics settings, and other scripts. Sample frame times before and after reloading the updated scripts, with no active random event. Repeat separately during enemy events. Script reset clears transient mod state and should be done from a safe location. Both patch and Fluffy ZIPs must include `BioRand7/object_cache.lua`.

The first live comparison on 2026-09-27 used the same stationary view, with the REF menu closed and build/tests finished. Two 21-sample MCP windows measured 63.6 FPS before and 103.3 FPS after script reload (frame-count deltas over wall time). Median sampled frame time fell from 15.49 to 10.32 ms. BioRand loaded without errors; existing component-array exceptions from an unidentified caller remained in the shared REF log. This is a short scene-specific comparison, not a guarantee for all locations or an isolated per-feature CPU profile; resetting scripts also resets other scripts' transient state.

A manually triggered enemy-speed event produced no BioRand errors, with a subsequent sample of 94.9 FPS. The live probe did not observe changed enemy time scales, so it does not establish that targets were affected or restored before the event expired. Native enemy-event behavior and an actual Em3300 encounter still need gameplay validation; their offline regression tests pass.

### Second performance pass

The follow-up audit covered the entrypoint and every feature/helper module:

| Area | Changes or retained behavior |
| --- | --- |
| Eveline | Remove duplicate validity checks during discovery; defer random-stream creation until proximity starts the countdown. Install the global think-update hook from the application callback only after discovering a target. The start hook still registers targets immediately. |
| Reload speed | Do not install the player-motion update hook when disabled. `Reload config` can install it later, once; precompute weapon configuration keys. Keep per-frame native rate writes when enabled. |
| Madhouse saves | Skip difficulty/singleton/object queries in both save-manager update hooks unless a menu is pending. Preserve their native timing. |
| Random events | Install weapon hooks on the first weapon event, once. Resolve damage controllers only for selected weak/strong targets. Poll passive-manager discovery every 0.25 seconds, applying deltas once per manager and restoring every touched manager. Reuse scale vectors while retaining per-frame writes. Freeze and enemy-effect writes remain per-frame. |
| Inventory | Inspect discard metadata only if the native result needs overriding; read item IDs only for protected categories. Keep allocation, combine, and Birthday-skill hooks event-driven. |
| Static Mia | Short-circuit on remembered GUIDs before reading folder/position or constructing fallback keys. Keep the fallback for empty/nonmatching GUIDs, without caching identities across pooled respawns. |
| UI/config | Sort and format config entries once per reload; poll runtime diagnostics at 4 Hz; reuse overlay vectors. No UI engine polling when its tree is closed. |
| Entrypoint/helpers | Pass update functions directly to `xpcall`, retaining feature-specific error isolation. Keep bounded object discovery, cached TDB definitions, and scene-lifetime checks. |
| Drops, knee-down, RNG/data, logging | Reviewed; already event-driven or pure Lua. Retain drop physics and seeded draw order, reaction guards, and error diagnostics. No speculative object-lifetime caches or broad registry hooks. |

REF does not document a Lua unhook API. Deferred hooks remain installed until ScriptRunner reset once they have been needed; their inactive guards still apply. Configuration reloads and state-clear buttons must not install duplicate hooks. A newly created passive manager may take up to 0.25 seconds to receive an active status effect; expiry and restoration are not throttled. The eight Lua suites include call-count/allocation checks and reload, hook-registration, suppression, and restoration regressions. Existing ZIP source-equality tests cover the changed embedded scripts; no additional runtime module is required in this pass.

The second live comparison on 2026-09-27 sampled 41 points over roughly 17 seconds per window, after builds/tests finished. With the same stationary view and menu closed, the fresh baseline was 112.6 FPS (median sampled frame time 8.65 ms); after reload the two windows were 113.7 FPS / 8.33 ms and 116.4 FPS / 8.65 ms. These small, variable differences do not establish a repeatable FPS gain. The 25% reduction in idle discovery calls and reductions in event/UI work are regression-tested, not inferred from these FPS samples. All 17 deployed scripts matched their repository sources, and BioRand reloaded without logged Lua errors. The pre-existing shared component-array exceptions continued.

A live, manually triggered infinite-ammo event changed `PlayerPassiveSkillManager.BulletStackNumInfinityCount` from 0 to 1. Once-per-second probes observed it remaining at 1 and returning to 0 at expiry. This verifies the deferred weapon-hook setup completed and the throttled passive-effect path activated/restored without accumulation in that session. It does not test bullet expenditure, other weapon events, enemy-event stress, scene transitions, or a marked Eveline encounter. No live game state was changed through MCP; the player triggered the event through the existing debug UI.
