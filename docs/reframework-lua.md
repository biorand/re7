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

The review's live MCP probes could not connect, so these native checks remain unverified. If an explosion cannot create a bomb or fallback effect, the script now records a diagnostic instead of silently discarding the failure.
