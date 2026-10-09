# Not a Hero and End of Zoe weapons

Status: experimental, **not campaign-certified**. Investigation: 2026-10-09.

This is the handoff for the DLC weapon integration attempt. Do not interpret the
source catalog, a successful export, an item-box entry, or a forced shot as a
working randomizer weapon. AMG-Dual is **not implemented for Ethan**.

Related: [integration plan](../DLCIntegrationPlan.MD),
[binary evidence](../DLCIntegrationEvidence.MD), [technical notes](../Notes.MD),
and [Lua API hazards](../reframework-lua.md).

## Implemented boundary

There are now two separate entry points, sharing `DlcWeaponImporter`:

- `DlcWeaponLabPatch` remains an explicit mod export under `BioRand/DlcWeaponLab`.
  Its manual Lua runner is not loaded by `BioRand7.lua` and grants nothing on load.
- `DlcCampaignWeaponPatch` integrates four candidates under `BioRand/DlcWeapons`
  when **both** `dlc-campaign-weapons` and `allow-dlc-items` are true. The new
  experimental option defaults to false. Ordinary profiles retain their old pools.
- The candidate set is Tactical Knife, Samurai Edge, Thor's Hammer, and Joe's M21.
  The remaining ten catalog entries, including AMG-Dual, stay excluded.
- Campaign integration includes item definitions, starting weapons and ammunition,
  random weapon pools, pickup templates, item settings/messages, inspection
  resource folders, and weapon-stat controls. Custom bird-cage entries are gated;
  the default cage table still disables ordinary guns.
- Inventory/detail PFBs and resource scenes are namespaced. CH8 gun parameters and
  knife attack RCOL are copied to that namespace before stat changes. Joe's M21
  deliberately shares campaign M21 parameters and controls. Source DLC settings
  and inventory PFBs are not rewritten, and DLC gameplay roots stay inactive.
- `_Data/dlc_weapon_assets.txt` lists 92 required installed resources, including
  weapon render/motion/collision/VFX dependencies and import metadata. Generation
  preflights every entry before registering anything and emits a clear error if
  the baseline needs `setup --dlc-weapons`. Extracted game assets are not committed.
- `BioRand7/dlc_weapons.lua` makes at most one deferred loading request per matching
  campaign prefab per session. It does not grant items, force native initialization,
  repair AI, or install attack/input hooks. Both flags imply REFramework is required.

This implements the campaign generation path, **not completed playthrough
certification**. Save migration is not implemented. Use backed-up test saves until
normal acquisition, storage, save/load, and all relevant player transitions pass.

### Preparing an opt-in seed

Use an installation with Not a Hero and End of Zoe, with generated/mod PAKs removed
from the setup input. Setup reads installed patch PAKs too: harvesting an already
randomized installation can contaminate the baseline and make patches fail their
expected-match checks. Do not weaken those checks to accept a dirty baseline.

```powershell
dotnet run --project src/biorand-re7 -- setup -i "<clean RE7 install>" -o out/dlc-baseline.pak --full --dlc-weapons
dotnet run --project src/biorand-re7 -- generate --input out/dlc-baseline.pak --config "<profile.json>" --seed 1685410476 --output out/dlc-seed
```

The profile must set `"allow-dlc-items": true` and `"dlc-campaign-weapons": true`.
Deploy a complete generated output with RE7 stopped; do not mix stale lab settings,
new inventory prefabs, and a different seed's runtime configuration.

## Source inventory

Names are resolved from source item messages, not inferred from asset names.
WeaponID is distinct from the string ItemDataID.

| ItemDataID | Source name | Chapter | WeaponID | Native component | Lab adapter |
| --- | --- | --- | --- | --- | --- |
| `CKnife` | Tactical Knife | CH8 | 48 | `app.Weapon` | Experimental knife |
| `Handgun_Albert_C` | Samurai Edge - AW Model-01 | CH8 | 49 | `app.CH8WeaponGun` | Experimental base gun |
| `Shotgun_Albert` | Thor's Hammer - AW Model-02 | CH8 | 50 | `app.CH8WeaponGun` | Experimental base gun |
| `Grenadebomb` | Grenade | CH8 | 58 | `app.CH8WeaponThrowable` | None |
| `Thermatebomb` | Incendiary Grenade | CH8 | 59 | `app.CH8WeaponThrowable` | None |
| `Stangrenadebomb` | Neuro-stun Grenade | CH8 | 60 | `app.CH8WeaponThrowable` | None |
| `CH9_WP000` | AMG-78a | CH9 | 61 | `app.CH9Weapon1600` | None |
| `CH9_WP001` | AMG-78 | CH9 | 62 | `app.CH9Weapon1600` | None |
| `CH9_WP002` | Spirit Blade | CH9 | 63 | `app.CH9Weapon1700` | None |
| `CH9_WP003` | Throwing Knife | CH9 | 64 | `app.CH9Weapon1500` | None |
| `CH9_WP004` | Throwing Spear | CH9 | 65 | `app.CH9Weapon1800` | None |
| `CH9_WP005` | Stake Bomb | CH9 | 66 | `app.CH9Weapon1900` | None |
| `CH9_WP006` | AMG-Dual | CH9 | 67 | `app.CH9Weapon1600` | None |
| `NumaItem072` | Joe's M21 | CH9 | 13 | `app.CH9WeaponGun` | Experimental base gun |

Additional identities that must not be conflated:

- `NumaItem071` is Joe's bare fists, using `CH9Weapon1600` and WeaponID 0. It is
  player support, not a fifteenth portable weapon.
- `CH9_WP007`, `CH9_WP008`, and `CH9_WP009` enum values do not establish usable
  weapons; no corresponding records were found in the inspected item settings.
- Spirit Blade's asset family is `wp1700_Hatchet`; the asset name is not its UI name.
- `NumaItem072` shares WeaponID 13 with campaign `Shotgun_DB`. A dictionary keyed
  only by WeaponID cannot represent both inventory identities independently.

Relevant native paths, relative to `natives/stm/`:

| Purpose | Path |
| --- | --- |
| Campaign item records | `prefab/item/resourceitemsettings.user.2` |
| CH8 item records | `ch8/prefab/item/resourceitemsettings_chapter8.user.2` |
| CH9 item records | `ch9/prefab/item/resourceitemsettings_chapter9.user.2` |
| CH8 messages | `ch8/message/ch8_item_mes.msg.17` |
| CH9 messages | `message/ch9_item_mes.msg.17` |
| Campaign messages | `message/ui_item_mes.msg.17` |
| CH8 resident weapons | `ch8/scenes/residentweapons.scn.20` |
| CH8 resource-folder index | `ch8/scenes/items/itemresources_chapter8.scn.20` |
| CH9 resource-folder index | `ch9/scenes/items/itemresources_chapter9 .scn.20` |

The space before `.scn` in the last path is real. Joe's M21 name message already
exists in the campaign message table. Merge messages by GUID and destination
language/attribute layout; copying a foreign message record verbatim can mismatch
the destination MSG schema.

## What the four adapters change

The gun adapters replace only the CH8/CH9 gun component with `app.WeaponGun`,
copying its serialized base fields. They retain the native WeaponID and source
weapon parameters. The knife already uses `app.Weapon`.

The export removes `app.DisableSave`, `app.CH8HandgunBulletSound`,
`app.CH8ReticleChanger`, and `app.CH8ReticleMaterialChanger` where present, and
permits item-box storage. Removing `DisableSave` is an experiment, **not proof of
persistence compatibility**. Source save GUIDs are not a certified identity policy.

For the gun experiment:

- `AlbertHandgunBullet` maps to campaign `HandgunBullet`.
- `AlbertShotgunBullet` maps to campaign `ShotgunBullet`.
- Existing enhanced campaign handgun ammunition remains available where present.
- `AlbertHandgunBulletL` (RAMRODs) is removed from the accepted ammo list. The base
  adapter does not reproduce CH8 anti-regeneration semantics.

The CH8 source stack limits are 90 normal handgun rounds, 120 shotgun rounds,
27 RAMRODs, and 6 per grenade type. CH9 throwing knives, spears, and stake bombs
have source stack limits of 9999. Those values are discovery evidence, not an
approved campaign balance policy.

## Live evidence and limitations

Runtime: RE7 `1.0.0.7`, executable SHA-256
`D550260029B51A580E206B9A12A4EB7F4A1B4DD230BBA5DEFCCA81DA40597321`,
REFramework `v1.5.9.1+507-d1461375`.

The HTTP/managed live endpoints timed out during the initial attempt. A temporary local
Lua command bridge executed bounded probes on `UpdateBehavior`. IDA MCP was not
reachable; the IDA facts linked above are from the earlier investigation, not a
new decompilation pass. Session-specific addresses are deliberately not recorded.

### Campaign adapter pass

The later pass used working read-only REFramework endpoints and a temporary
`UpdateBehavior` command bridge for mutations. A cold restart loaded the staged
110-file campaign export (92 dependencies plus 18 adapted/merged files).

| Check | Observation | Remaining limitation |
| --- | --- | --- |
| Full seed generation | Completed with a clean baseline augmented by the manifest; starting loadouts selected the CH8 candidates and an extra chest selected Joe's M21 | The full generated seed was not deployed for a playthrough |
| Cold prefab load | All four campaign handles became `Ready=true`, `Valid=true` without manual preparation | Readiness is not functional certification |
| Samurai Edge ownership | Native item-box withdrawal created an inventory-owned pistol; inventory equip selected WeaponID 49 | Acquisition used a temporary boxed test grant, not a world pickup |
| Samurai Edge firing | Normal mouse input reached native `shoot`/`fireBullet` for ID 49 and consumed rounds | Shot timing and balance are not certified |
| Empty-magazine reload | Normal attack input on an empty magazine triggered the reload animation; 0 loaded / 30 reserve became 9 / 21 | Dedicated reload-key automation was inconclusive |
| Normal Molded damage | User observed lethal headshots and lower body damage with Samurai Edge | Not a controlled damage/balance measurement; balancing deferred |
| Tactical Knife | Box withdrawal and equip reached ID 48 / bank 30 | Hand pose and attack behavior were not certified |
| Thor's Hammer | Box withdrawal produced the correct inventory ID | The session later crashed; firing/reload were not validated |
| Joe's M21 | Serialized adapter and generated chest passed checks | No clean live equip/fire/reload pass yet |

Two later sessions were invalidated by direct inventory/menu probes: a vanilla
weapon storage call threw and was followed by process exit; another session
developed an invalid native inventory reference before exiting after shotgun
withdrawal. The latter crash log included native finalization frames. These facts
do **not** isolate a weapon defect or exonerate the adapters. Do not count either
session as a storage, persistence, or multi-weapon compatibility pass. The temporary
bridge and diagnostic hooks are not part of the production module.

Save files matched the pre-test backup after these sessions. No DLC-bearing save
was written, so that comparison is preservation evidence, not a save/load test.

### Earlier lab evidence

| Check | Observation | What it does not prove |
| --- | --- | --- |
| Source discovery | All 14 IDs match source settings, components, and WeaponIDs | Campaign usability |
| Compiled export | Deterministic output; four candidate IDs, correct base types/ammo, render/motion components retained | Runtime lifecycle or complete asset closure |
| Item-box insertion | `ItemBoxData.addItem` accepted the experimental pistol | Instantiation, withdrawal, or equip |
| Raw CH8 pistol on Ethan | `_CH8PlayerStatus` was null; campaign had no `CH8ShellManager` | That every gun failure has the same cause |
| Forced adapted pistol | Native `shoot`/`fireBullet` ran; magazine decreased 7 to 6 with trainer infinite ammo disabled | Normal inventory equip, correct animation, reload, damage, persistence |
| Imported prefab readiness | Four imported handles reported `Exist=true`, `Standby=true`, but `Ready=false`, `Valid=false` in a fresh campaign | A missing physical file |
| Explicit knife load | Standby off, same path reassigned, standby on; a later probe saw `Ready=true`, `Valid=true`, then instantiated the prefab | Inventory ownership or attack readiness |
| Deferred knife insertion | `isCanAddItem` returned true; `addItem(app.Item, via.GameObject)` with a null out argument threw | Whether the cause is the Lua out-argument marshalling or native item setup |
| AMG-Dual | Source and player dependency chain identified | A working campaign gauntlet |

The forced pistol experiment included temporary motion-bank/name experiments and
did not use a normal inventory-owned weapon throughout. It is useful evidence of
the base bullet path, but must not be reported as a clean compatibility pass.
These historical lab results are superseded only where the campaign-pass table
above records a stronger check; they must not be generalized to all four weapons.

The campaign bullet RCOL already includes `Handgun_Albert_C` damage 300,
`Handgun_Albert_C_L` damage 1000, and `Shotgun_Albert` damage 60. The inspected base
gun path supports bullet types 20/21 for WeaponID 49 and 22 for WeaponID 50.
These are source/runtime mapping observations, **not measured enemy damage**.

Original Ethan motion-bank probes returned 48 -> 30, 49 -> 100, 50 -> 120,
13 -> 110, and 58/61..67 -> 0. Campaign Albert IDs 9/46 returned 101 and M37 ID 11
returned 160. Do not globally rewrite WeaponIDs to borrow animations: identity
also drives bullet type, parameters, saves, and equip behavior. A bank mapping
alone does not prove that all required motions exist or transition correctly.

## Why AMG-Dual is different

The gauntlet inventory prefab has Transform, Item, and `CH9Weapon1600`; it is not
a self-contained pair of animated weapon meshes. The earlier IDA trace shows
`CH9Weapon1600.onInsertInventory` resolving `CH9PlayerEquipManager` from its owner
and registering a knuckle weapon.

That manager uses `ICH9PlayerOrder`, `CH9PlayerWeaponChange`, and
`CH9PlayerMeshController`. Actual punching is implemented by
`CH9PlayerKnuckleWeapon : WeaponGun` on Joe's player/hand system. Its references
include `ICH9PlayerStatus`, `ICH9PlayerOrder`, and `CH9PlayerSequenceManager`; it
switches default/gauntlet IDs and has separate left/right-hand and stomp behavior.

Ethan lacks this input, combo/charge, sequence, hit-window, and mesh-switching
chain. Replacing `CH9Weapon1600` with `app.Weapon` would not implement AMG-Dual.
The remaining investigation is a player-system adapter, including cancellation,
damage reactions, guarding, scripted interactions, and restoring Ethan's state.
Do not activate a whole Joe player/gameplay root to satisfy one missing interface.

Other blocked families have distinct requirements:

- CH8 grenades use standby, pin-pull, timed throw, underthrow, stock consumption,
  collision adjustment, and `CH8ShellManager` grenade pools.
- CH9 throwing knives and spears derive from `CH9WeaponThrowable`, with their
  own equip/throw/use-type lifecycle. They are not conventional `WeaponGun`s.
- Spirit Blade has `RequestSetCollider`, CH9 owner status/order, attack-hit checks,
  and recovery values. A knife mesh swap would omit its behavior.
- Stake Bomb also has `CH9WeaponLiquidBombAppend`; a campaign remote-bomb ID is
  not automatically an equivalent implementation.

## Pitfalls to carry forward

1. **Prove the test environment first.** Trainer infinite ammo, instant kills,
   frozen item-box counts, and random-event ammo bonuses were confounders here.
   Record and restore their settings. Confirm the native infinity flag as well
   as the HUD before interpreting ammunition results.
2. **Separate registration, resource loading, initialization, and ownership.**
   Loaded `ItemSettings._Settings` alone did not populate `_SettingsSearch` in a
   dynamic test; `setup()` did. The serialized lab uses the normal campaign load
   path instead of globally replacing settings at runtime.
3. **Prefab existence is not readiness.** Check `Path`, `Exist`, `Ready`, and
   `Valid`; then wait for native callbacks. Do not force awake/start or perform
   creation, inventory resizing, insertion, and equip in one callback.
4. **A filtered dump is not a component inventory.** `SceneProbe` prints only
   matching components. The CH8 PFBs do contain render/motion components, also
   visible in `residentweapons.scn`. An initial inference that they were stubs
   was disproved. Inspect the full component list before adding a hydration step.
5. **Respect actual TDB signatures.** `ItemManager.createItemInstance` is static.
   Generated C# `out GameObject` is not a literal Lua `via.GameObject&` signature.
   The raw method is `addItem(app.Item,via.GameObject)`, but finding it does not
   validate null out-argument marshalling. `Inventory` uses real
   `get_ItemSlotManager`/`get_ItemBoxData` accessors, not fields with those names.
6. **Do not use menu shortcuts as compatibility evidence.** Calling only
   `MenuManager.openItemBoxMenu` produced placeholder UI rather than an initialized
   inventory. A manual `InventoryMenu.setup` attempt threw. Reload after these
   failures; the lab intentionally has no open-box command.
7. **Treat failed mutations as terminal for that experiment.** A combined slot
   resize/insertion/equip experiment threw and then crashed RE7. The precise cause
   was not isolated. No resizing, direct insertion, forced equip, or retry loop is
   included in the committed helper.
8. **Use the smallest resource scope.** An isolated CH8 shell manager experiment
   initialized 30 bullets and 6 shells per grenade type, but this was not a full
   gameplay proof. The source `CH8_SystemObject` also owns mode services; importing
   it wholesale risks rank/mission/singleton interference.
9. **Keep item and interaction IDs synchronized.** Future pickups need both
   `app.Item.ItemDataID` and independent `app.fsm.ItemAddTest._ItemDataID` paths
   rebound. Native `ItemResource` registration/detail-search resources are a
   separate requirement from inventory prefab registration.
10. **Certification and selection are separate.** Keep the experimental campaign
    gate independent of DLC and unlockable permission. The full source catalog is
    not a usable weapon pool. `Shotgun_DB` stays the canonical reverse lookup for
    WeaponID 13; Joe's M21 is a separate inventory ID but shares stats/ammunition.
    Non-repeating selections treat the two IDs as aliases.
11. **Cached templates may already be wrong for the new mode.** The embedded
    template scene already contains raw DLC objects. A create-only-if-missing
    branch silently reused them. Campaign adapters explicitly replace those
    cached entries once. Donor gun components must use the imported native ID,
    not merely the new ItemDataID and mesh. Tactical Knife uses the MiaKnife pickup
    shell because the ordinary Knife template lacks the required interaction child.
12. **Preserved FSM pickups need identity repair too.** Replacing a template only
    covers cloned pickups. Preserved objects and configured bird cages also need
    their existing weapon component rebound. Matching local ItemAddTest IDs are
    rewritten without changing object GUIDs or FSM UIDs. External parent/sibling
    FSM references still require scene-specific runtime validation.
13. **Check raw field names and offsets.** `InventoryItemIcon.ParentItemParam` is
    backed by `<ParentItemParam>k__BackingField`, not a same-named field. At the
    inspected layout, InventoryMenu offset `0x2A0` is `_SelectedMoveItem`, not
    `_FocusItemIcon` (`0x298`). A plausible-looking generated property or nearby
    offset is not enough to justify a mutation.
14. **Lua assertions can change native argument counts.** `assert(value, message)`
    returns both arguments. At the end of a Lua argument list, it can accidentally
    pass the message to `object:call` as another native argument. Assert separately
    or parenthesize the expression. Validate against the live TDB signature.
15. **Input and capture failures are not weapon failures.** Short synthetic key
    taps were inconsistently consumed by the game, including vanilla controls.
    Prefer an observed native action and ammo transition over assuming a key was
    received. Do not force reload/shoot and then call it normal-input validation.
16. **Keep scope and parameter ownership explicit.** Readiness repair does not
    supply missing meshes or sounds. Copy the dependency manifest and resource
    registrations first. Isolate CH8 parameter/knife RCOL edits from source DLC;
    document deliberate aliases such as Joe's M21. The added campaign
    WeaponMotionController was not independently A/B-tested as a causal fix.

These are the same broad lessons as Em4400: serialized component parity does not
prove native initialization, and one successful animation does not prove a
complete behavior chain. Keep observations separate from root-cause hypotheses.

## Running the isolated lab

Use a disposable save/profile and back up saves and overridden loose files first.
Installed source DLC assets are required; the output is **not self-contained**.
Do not redistribute extracted game assets. The export writes campaign settings
and messages, so do not assume it composes with an arbitrary generated PAK or
another loose-file override.

```powershell
dotnet run --project src/biorand-re7 -- mod -m "DLC Weapon Lab" -i "<RE7 install>" -o out/dlc-weapon-lab
```

1. Deploy the explicit lab export using the normal mod workflow, with RE7 stopped.
2. Ensure the current `BioRand7/game.lua` and `BioRand7/dlc_weapon_lab.lua` modules
   are deployed. Run `tools/dlc_weapon_lab.lua` manually via ScriptRunner; do not
   add it to ordinary autorun or grant weapons on load.
3. Load Ethan's campaign. Enable commands, select one candidate, and use
   **Prepare prefab** once. Wait, then **Add to item box**. A not-ready error
   disarms the lab; investigate it instead of repeatedly forcing initialization.
4. Use a real item box and free the required slots normally. Test withdrawal,
   inspection, equip, attack, empty-magazine behavior, reload, ammo switching,
   re-equip, storage, and scene transitions. Inspect native weapon state too.
5. Return the test weapon to storage before **Remove boxed test weapons**. Only
   additions recorded in the current lab session are eligible for removal.
   New-game/load/script-reset events discard receipts; after that, reload an
   uncontaminated save instead of guessing which items belong to the experiment.
6. Do not save a production playthrough. Remove lab overrides with RE7 stopped,
   restore previous overridden files/settings, and restart to clear cached
   resources and temporary hooks.

The helper queues commands outside draw callbacks, executes each once, checks the
Ethan owner and exact lab prefab path, refuses duplicates, and disarms on errors.
Preparation requests loading only; it neither certifies the prefab nor repairs
the rest of the native lifecycle.

## Acceptance and next steps

Automated coverage includes the 14 source identities, rejected unsupported
adapters, default-off behavior, both permission gates, manifest extraction and
missing-resource preflight, deterministic lab export, serialized native identities,
pickup interaction rebinding, messages, ammo mapping, capacity changes, and the
WeaponID 13 alias. Lua coverage checks exact namespace guards, deferred bounded
loading, failures without repeated mutations, and load/new-game invalidation.

The campaign integration full solution run passed 593 tests, and all 19 Lua suites
passed under Lua 5.4. The machine's `lua` command was Lua 5.1, so a compatible
Lua 5.4 runtime was used. Automated success does not remove the manual risks above.

Remaining acceptance work:

1. Reproduce normal pickup and item-box operations with no direct inventory/menu
   probes, starting from clean sessions. Isolate the later invalid-reference crash.
2. Test each candidate's equip, normal attack, enemy damage, reload, ammo switching,
   inspect view, re-equip, and two-way storage. Check the knife's hand pose explicitly.
3. Write and reload a disposable DLC-bearing save; cross scene/chapter boundaries,
   item-box transfers, Mia and VHS/Clancy transitions, and title-menu reloads.
4. Validate all candidate pickup families in a complete generated seed, including
   parent-controlled FSMs and any explicitly enabled bird-cage entries. Check sound
   loading separately; the curated manifest is not proof of a complete sound-bank
   closure. Check randomized loaded ammo on scene pickups as well as inventory PFBs.
5. Keep AMG/throwable/blade player-system ports deferred. They are not required to
   finish this four-candidate campaign integration, and no runtime shim for them
   should be hidden inside the conventional gun adapter.
