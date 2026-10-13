# Not a Hero and End of Zoe weapons

Status: experimental, **not campaign-certified**. Investigation: 2026-10-10.

This is the handoff for the DLC weapon integration attempt. Do not interpret the
source catalog, a successful export, an item-box entry, or a forced shot as a
working randomizer weapon. AMG-Dual, AMG-78a, and AMG-78 now join the opt-in
campaign pool through an experimental player adapter. Combat, acquisition, and
persistence evidence below is narrower than complete playthrough certification.

Related: [integration plan](../DLCIntegrationPlan.MD),
[binary evidence](../DLCIntegrationEvidence.MD), [technical notes](../Notes.MD),
and [Lua API hazards](../reframework-lua.md).

## Implemented boundary

There are now two separate entry points, sharing `DlcWeaponImporter`:

- `DlcWeaponLabPatch` remains an explicit mod export under `BioRand/DlcWeaponLab`.
  Its manual Lua runner is not loaded by `BioRand7.lua` and grants nothing on load.
  The export preflights and copies the weapon dependency manifests, including 38
  gauntlet display, hand, material, texture and motion resources from the installed game.
- The lab and campaign now have fourteen candidates, including the three
  CH8 grenades, CH9 throwing knife/spear, and Stake Bomb. Grenades retain native `CH8WeaponThrowable`, lose `DisableSave`,
  keep six-item stacks, and receive isolated shell pools, copied messages, and
  inspection resources. The campaign uses its own namespace and automatic runtime
  adapter; it does not run the manually armed lab controller.
- The lab also exports a **Spirit Blade base-melee adapter**.
  It preserves WeaponID 63 and the source render/motion/collider stack and
  includes opt-in hand-axe motions and confirmed-hit recovery. The campaign
  version additionally isolates its attack RCOL and participates in generation
  and debug grants. Lab candidacy and campaign candidacy remain separate
  properties; do not promote a lab export merely because its prefab loads.
- `DlcCampaignWeaponPatch` integrates fourteen candidates under `BioRand/DlcWeapons`
  when **both** `dlc-campaign-weapons` and `allow-dlc-items` are true. The new
  experimental option defaults to false. Ordinary profiles retain their old pools.
- The candidate set is Tactical Knife, Samurai Edge, Thor's Hammer, Joe's M21,
  Spirit Blade, AMG-78a, AMG-78, AMG-Dual, Grenade, Incendiary Grenade, and Neuro-stun
  Grenade, Throwing Knife, Throwing Spear, and Stake Bomb.
- Campaign integration includes item definitions, starting weapons and ammunition,
  random weapon pools, pickup templates, item settings/messages, inspection
  resource folders, and weapon-stat controls. Custom bird-cage entries are gated;
  the default cage table still disables ordinary guns.
- Inventory/detail PFBs and resource scenes are namespaced. CH8 gun parameters and
  knife attack RCOL are copied to that namespace before stat changes. Joe's M21
  deliberately shares campaign M21 parameters and controls. Source DLC settings
  and inventory PFBs are not rewritten, and DLC gameplay roots stay inactive.
- `_Data/dlc_weapon_assets.txt`, `_Data/dlc_gauntlet_assets.txt`,
  `_Data/dlc_grenade_assets.txt`, and `_Data/dlc_ch9_throwable_assets.txt` together
  list 320 required installed resources,
  including weapon render/motion/collision/VFX/sound
  dependencies and import metadata. Generation
  preflights every entry before registering anything and emits a clear error if
  the baseline needs `setup --dlc-weapons`. Extracted game assets are not committed.
- `BioRand7/dlc_weapons.lua` makes at most one deferred loading request per matching
  campaign prefab per session. It does not automatically grant items, force native
  initialization or repair AI. Its scoped player adapter maps Spirit Blade's
  motion bank and observes confirmed damage for recovery. The separate gauntlet
  adapter maps native melee actions to Joe's clips and enables their exported
  collision windows. The grenade adapter drives the native throw callbacks and
  owns a scoped shell pool. The CH9 knife/spear adapter reads native throw-timing
  tracks and owns a stationary native CH9 shell pool. Stake Bomb's separate Item
  motion adapter shares that pool and retains native placement, recovery and
  detonation. None of these adapters
  synthesizes player input. Both flags imply
  REFramework is required.
- **BioRand 7 > Debug tools > Add supported DLC weapons to item box** queues a
  one-shot grant on `UpdateBehavior`. It adds one of each of the fourteen candidates,
  skipping weapons already owned in inventory or storage. Both integration flags,
  campaign Ethan, and all fourteen ready campaign prefabs are required. Missing/foreign
  adapters abort before any additions; a native add failure stops the batch without
  retrying or removing earlier successes. The menu reports the result. Loading a
  save, starting a new game, resetting scripts, or reloading config cancels pending
  requests. The button does not deploy assets, equip weapons, or save the game.
- Grenades are stack weapons, not guns or ammunition. The Explosives starting
  category includes them only with both flags enabled. Each has an independent
  item/enemy drop weight (default 0.03), gated at pool construction and relief-table
  export. A drop supplies one grenade. Disabled-only grenade weights fall back to
  Herb for both ordinary pickups and crates, rather than requesting an unavailable
  pickup template. Their isolated RCOLs participate in damage/stun controls, respecting
  Include Self-Damage; contact-only zero-damage requests retain their native values.
- Knives and spears retain native `CH9Weapon1500`/`CH9Weapon1800`, six-item stacks,
  namespaced inventory/inspection resources, and isolated projectile RCOLs. Their
  loose pickup donor is `LiquidBomb` (`InteractDetailSearch`), not a permanent
  melee weapon (`InteractWeapon`). A preserved gun pickup instead receives the
  concrete native CH9 component. They participate in static pools, crate relief
  tables, runtime enemy drops, damage/stun controls, and debug grants. They do not
  replace the permanent Bladed starting weapon with consumable stock.
- Stake Bomb retains native `CH9Weapon1900` and `CH9WeaponLiquidBombAppend`, with
  six-item stacks, a LiquidBomb loose-pickup donor, isolated installation RCOL,
  inspection resources, and item-box persistence. It joins the opt-in Explosives
  starting category and has independent item/enemy drop weights (default 0.03).
  Damage/stun controls preserve native enemy/player explosion requests and respect
  Include Self-Damage. Its `Damage` request is damage-receiver data, not an attack
  stat to scale. The source DLC RCOL is never rewritten.
- The six consumable DLC weapons have runtime Lua drop entries as well as C#
  drop definitions, gated by both flags. Boss reward eligibility remains unchanged.
  CH9 drop config IDs use `ch9-wp003`/`ch9-wp004`/`ch9-wp005`, not native item-ID underscores;
  generation and Lua must normalize identically. The native `AttackToWeapon`
  requests on knife/spear projectiles intentionally have zero damage and stun.

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
| `Grenadebomb` | Grenade | CH8 | 58 | `app.CH8WeaponThrowable` | Experimental campaign throw adapter |
| `Thermatebomb` | Incendiary Grenade | CH8 | 59 | `app.CH8WeaponThrowable` | Experimental campaign throw adapter |
| `Stangrenadebomb` | Neuro-stun Grenade | CH8 | 60 | `app.CH8WeaponThrowable` | Experimental campaign throw adapter |
| `CH9_WP000` | AMG-78a | CH9 | 61 | `app.CH9Weapon1600` | Experimental campaign punch/charge adapter |
| `CH9_WP001` | AMG-78 | CH9 | 62 | `app.CH9Weapon1600` | Experimental campaign punch/charge adapter |
| `CH9_WP002` | Spirit Blade | CH9 | 63 | `app.CH9Weapon1700` | Base melee: Ethan attack/damage/recovery and cold campaign save/load verified |
| `CH9_WP003` | Throwing Knife | CH9 | 64 | `app.CH9Weapon1500` | Experimental campaign throw adapter |
| `CH9_WP004` | Throwing Spear | CH9 | 65 | `app.CH9Weapon1800` | Experimental campaign throw/recovery adapter |
| `CH9_WP005` | Stake Bomb | CH9 | 66 | `app.CH9Weapon1900` | Experimental campaign Item placement/recovery adapter |
| `CH9_WP006` | AMG-Dual | CH9 | 67 | `app.CH9Weapon1600` | Experimental campaign punch/charge adapter |
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

## What the adapters change

The gun adapters replace only the CH8/CH9 gun component with `app.WeaponGun`,
copying its serialized base fields. They retain the native WeaponID and source
weapon parameters. The knife already uses `app.Weapon`.
Spirit Blade replaces its CH9-specific weapon component with `app.Weapon`, keeps
the source mesh/motion/collider stack, and uses the scoped player adapter below.

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
| Thor's Hammer | Later clean session: native box withdrawal/equip, normal mouse fire, campaign Molded damage, and empty-magazine reload passed | Full pickup/player-transition and sound validation remain open |
| Joe's M21 | Later clean session: native box withdrawal/equip, normal mouse fire, campaign Molded damage, and empty-magazine reload passed | Shares campaign M21 identity/parameters; not independently balanced |

Two later sessions were invalidated by direct inventory/menu probes: a vanilla
weapon storage call threw and was followed by process exit; another session
developed an invalid native inventory reference before exiting after shotgun
withdrawal. The latter crash log included native finalization frames. These facts
do **not** isolate a weapon defect or exonerate the adapters. Do not count either
session as a storage, persistence, or multi-weapon compatibility pass. The temporary
bridge and diagnostic hooks are not part of the production module.

Save files matched the pre-test backup after these sessions. No DLC-bearing save
was written, so that comparison is preservation evidence, not a save/load test.

The later eight-weapon cold-load session supersedes those two shotgun gaps only:
both shotguns were withdrawn through the physical item-box UI, with Thor's Hammer
retaining 12 loaded rounds and Joe's M21 retaining two. One 30-shell test stack was
added to storage with menus closed, then withdrawn through the native UI. Both
guns reported ordinary `ShotgunBullet`, finite magazine and finite reserve flags.
Normal mouse shots consumed one loaded shell each and reached campaign
`DefaultBullet` / `EnemyDamageController` processing. An observed Thor hit totaled
300 raw / 150 added damage against an Em4000; an M21 hit totaled 720 raw / 1260
added damage against another Em4000. Pellet aggregation and hit zones differ, so
these numbers establish working damage, not a balance comparison. The targets
were existing campaign spawn records positioned for a disposable combat test;
enemy health was not edited. Player incoming damage was disabled for the test.

After normal shots emptied each weapon, another mouse attack initiated its native
reload sequence. M21's two one-shell callbacks changed 0 loaded / 30 reserve to
2 / 28. Thor's magazine callback changed 0 / 28 to 12 / 16. No direct gun reload,
ammo setter, or forced fire call was used. A short synthetic reload-key tap did not
produce a reliable action, so dedicated key handling remains a separate check.
This pass did not save its ammo consumption or certify shotgun world pickups.

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
| AMG-Dual | Lab punches/charges damage campaign Molded; cold equipped load with explicit dependencies | Complete campaign lifecycle and normal held-button charging |

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

### Additional source evidence (2026-10-10)

Joe's complete player object is serialized as `Pl9000` in
`ch9/scenes/chapter/c09_ingame.scn.20`, not `chapter9resident.scn.20`.
Its prefab reference is `CH9/Prefab/Character/Pl9000/Pl9000.pfb`. This scene
provides the complete component inventory even when that prefab is absent from
the local extracted subset. The motion uses `pl9000.motbank` and
`pl9000_JointMap.jmap`; its ten FSM layers include Body, Weapon, RHand,
TouchWall, PosturalCamera, DamageCamera, AddBlend, and WeaponUpper resources.
These are player resources, not dependencies contained in the gauntlet item PFB.

Live static TDB fields give Joe's bank IDs: bare knuckles 5, AMG-78a 6,
AMG-78 7, AMG-Dual 8, Spirit Blade 10, throwing knife 300, throwing spear 310,
and liquid bomb 230. Chris's grenade/incendiary/neuro-stun banks are 260/270/280.
These constants establish mappings, not that Ethan's bank contains those motions.
Spirit Blade's bank 10 is the same family as the campaign hand axe.

`CH9/Prefab/Weapon/CH9ThrowWepParam.user` supplies Spirit Blade recovery values
100 and 150 (`Wp1700RecoveryValueS/L`). Its source weapon owns CH9 status/order
references and `checkAttackHit()`; the base-melee lab conversion omits that native
healing path. Healing must be driven by confirmed hits, not clicks or every frame
of an active attack. Do not change the inventory/native WeaponID to borrow a bank.

The 2026-10-10 cold campaign load registered the isolated Spirit Blade PFB,
accepted its deferred load request and an item-box entry. Normal item-box withdrawal
and the examine model worked. Ethan equipped native ID 63 and accepted mouse-driven
melee attacks with hand-axe bank 10. Subsequent hallway combat confirmed native
damage against a campaign `Em4000`: a 100-point base hit reduced health by 60,
after its normal resistance. Request sets 0/2/4 activated during `Melee.AttackL`
and unregistered when the swing ended; `IsAttacking` also returned to false.

The bank hook must exist **before equip**. Equipping unsupported ID 63 first entered
`Melee.ReadyStart` with frame/end-frame both zero. Changing the bank afterwards did
not restart the animation, and subsequent `Inventory.equipWeapon` calls stayed
pending (`isUseItemRequested=true`, `isUseItemTried=false`). This initially looked
like an absent axe motion resource. In fact, `getActiveMotionBankCount` included
`pl0000_Ax`, bank type 10; `getMotionBankCount` returned zero and was not a valid
inventory of the active banks. A one-shot research restart of the already-stalled
ready state proved the resource worked. The checked-in adapter does **not** force
FSM states: installed before a fresh knife-to-blade equip, it allows the native
ready/idle/attack transitions to complete normally. `dlc_weapon_player.lua` changes
the bank return for ID 63 and the exact imported prefab path. It accepts active
campaign Ethan (`Pl0000`, `Pl0000_Chapter1`), Mia (`Pl2000`, `Pl2100`) and VHS
Clancy (`Pl3000`), selecting available bank 10 or falling back to bank 30. Ship Mia
uses Ethan's complete bank. In the loaded ship save, `Pl2100` equipped native ID
63 and completed a mouse-driven swing with request sets 0/2/4, returning to idle
with no registered hit sets. This was a staged inventory test, not normal pickup
or storage certification; direct menu calls threw after partially doing their
work. Do not retry a failed native inventory call without inspecting ownership.
Continuous story transitions and VHS validation remain open.
Bank availability also needs an exact identity check: in live Ethan,
`findMotionBank(0, 9911)` returned `pl0000_Hands` with bank ID/type 0/0 even though
type 9911 did not exist. A non-null result is not proof that the requested bank
exists. The adapter verifies the returned `BankID` and `BankType`; its regression
test models this fallback instead of returning null for an absent bank.
Do not confuse `Pl3100_Chapter7_#` with Mia: these belong to the Bedroom, 21 and
Nightmare DLC scenes, as the area catalog confirms. They remain excluded.
The lab must be armed; the campaign uses both config flags.
It does not rewrite weapon identity or alter DLC players.

The shared recovery adapter observes `DamageController.addDamageCore`, whose returned
`DamageRecord.AddedDamage` and receiver health change both confirmed real damage
in the live test. The record's `CalculatedDamage`, `AddedDamage`, and `DamageInfo`
are fields; `get_DamageController()` is a real accessor. Recovery requires positive
added damage, enemy health loss, the tracked weapon attacker, and the current
player identity. It queues 100.0 health (150.0 for an aimed attack) for UpdateBehavior,
with one recovery per attack window and no native hit-object retention between
frames. Loads, disarming, and weapon/player changes invalidate pending recovery.
A live light strike healed Ethan from 500 to 600 HP; a deliberate wall swing left
health at 600, and the next landed strike healed to 700. Aimed recovery still
requires runtime validation. The solution built with zero warnings
or errors, and all 22 Lua 5.4 suites passed after this adapter was added.
`Weapon.onAttackTrigger/offAttackTrigger` hooks did not fire for this observed
attack path, so they are not used as the recovery reset mechanism.

A normal manual save retained Spirit Blade in inventory and equipped state. A
fresh process loaded it successfully with the campaign adapter: ID 63, campaign
prefab `Ready=true`, bank 10, pause zero, and a normal mouse attack returning to
idle. The first lab-only cold load stalled until a deferred prefab request was
made; serialized `Standby=true` alone was insufficient. The campaign preload must
include Spirit Blade before inventory restoration waits on that handle. Do not
mistake this loading-screen stall for a corrupt save. Damage randomization changes
only the namespaced attack RCOL; source CH9 RCOL bytes remain unchanged.

Combat test setup matters: a spawned Molded immediately self-suspended in the
safe room even after disabling its explicit self-suspend option. The same setup
stayed active in the adjacent hallway. The staged human `Em3100` was not a valid
damage control: even a vanilla G17 shot produced no damage there. Do not infer a
weapon RCOL failure from an unverified target. The eventual test protection only
disabled incoming player damage; enemy resistance and health handling stayed native.

A separate deployment check
found current campaign assets alongside an older installed `BioRand7.lua` that
did not require or update `dlc_weapons.lua`; enabling the config alone therefore
did not load the four existing prefabs. Deploy the entrypoint and feature modules
together. After the matching entrypoint was loaded, all four became ready.

### Player-system requirements

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

Joe's serialized right/left hand children use `pl9010.mesh` / `pl9020.mesh` and
matching materials, not a mesh in the inventory prefab. Their gauntlet emissive
materials are controlled by `CH9PlayerMeshController.setGantletParts`. The base
player's hand meshes and the gauntlet variants therefore need a reversible visual
adapter, in addition to attack logic. The native motion resource API successfully
loaded Joe's Knuckle (53 motions), GauntletW (64), Gauntlet (26), and GauntletR (26)
lists into unused diagnostic dynamic banks on Ethan. This proves resource loading,
not animation retargeting or gameplay. Finite raw punches exist alongside blend
trees: e.g. right straight 2440 (60 frames) and its hit reaction 2565 (28 frames).
Never treat a blend-tree end frame of -1 as a missing resource.

### AMG-Dual campaign prototype

The lab exporter now has a sixth asset candidate, `CH9_WP006` / native WeaponID
67, but it remains excluded from campaign pools and debug grants. The stock lab
helper does not yet expose it: the following observations used an isolated,
temporary player adapter, not a complete shipped implementation.

- Converting the inventory component to base `app.Weapon` avoids Joe's
  `registerKnuckleWeapon` callback. Campaign inventory recognized a one-slot,
  quick-selectable weapon with icon 905, its name and description. Native box
  withdrawal and equip were exercised; normal two-way storage and cold save/load
  are not yet certified. The original description still mentions Extreme Challenge.
- Joe's GauntletW motion list is an override, not a complete bank. It lacks idle
  and the first punch. Three owned dynamic banks with the same isolated type,
  ordered GauntletW, Knuckle, then Ethan's axe fallback, produced a normal
  `ReadyStart -> ReadyIdle -> AttackL -> AttackLToReady -> ReadyIdle` path.
  Base campaign banks were not overwritten. Special idle also completed normally.
  A temporary gap while replacing the research hook stranded an existing idle
  motion; never disable a bank mapping underneath an equipped weapon.
- Joe's hand meshes/materials can replace Ethan's existing hand renderers and
  follow his animated joints. Restore the exact original resources and part mask
  on unequip. Enabling all gauntlet parts was only a visual loading proof, not a
  validated variant selection.
- The inventory prefab has no skeleton or collision components. The exporter
  adds the ordinary melee HitController/RequestSetCollider component pair and an
  isolated subset of Joe's player RCOL: right/left hooks, right/left uppercuts,
  the two both-hand charge levels, and the left straight finisher. Request IDs
  are compacted to 0 through 6 (the finisher is 6);
  native attack userdata, shapes and bone names remain intact. Source RCOL is not
  changed.
- Do **not** clear the RCOL joint names and attach these shapes to a palm. Joe's
  capsules use upper-arm/forearm-local coordinates. A hidden mesh on the weapon,
  using the active player's body mesh/material, an identity local transform,
  empty attachment joint, and SameJointsConstraint provided both arm skeletons.
  All six tested arm/palm joint positions exactly matched Ethan. Joe's right-hand
  mesh alone supplied only right-arm bones and was insufficient for Dual-AMG.
  The serialized Transform field is spelled `SameJointsContraint`; the live
  accessor is `set_SameJointsConstraint`.
- A mouse-driven prototype punch registered the native right-hand attack only
  during its bounded animation window. `addDamageCore` recorded native attacker
  `wp1620_GauntletW_Item`, raw damage 300, calculated/added damage 180, and a
  matching 180 HP loss on a normal campaign Em4000. The collider unregistered and
  the attack returned to idle. Enemy resistance was unchanged; test protection
  blocked only incoming player damage. This proves native combat routing, not
  exact timing parity with Joe.

Further runtime findings from the isolated adapter:

- `PlayerMotionController.updateLArmMotion` both selects a motion work entry and
  dispatches it to the motion FSM. Copying the right-arm entry after this method,
  or skipping it without dispatching the replacement, leaves the left arm in
  `RemoveWeapon`. A scoped equivalent dispatch synchronized layers 1, 2 and 9.
- The native CH9 mesh controller selects parts 2 and 3 for non-VR dual gauntlets,
  with parts 0, 1 and 4 disabled. This replaced the all-parts loading experiment.
- Read `app.Collision.ColliderTrack` from each active raw `MotionNodeCtrl` through
  `getSequenceTracks`. A successful read alone does not mean an active hit window:
  `RequestId` is -1 outside it. Right/left hook windows were observed around frames
  26 through 31, later than the initial approximate test window.
- Direct `TreeLayer.changeMotion` is not a complete FSM transition. Raw punches
  can finish while the FSM remains in `Melee.AttackL/R`. A one-shot normal-priority
  request to the corresponding `AttackL/RToReady` state returned both tested hooks
  to idle. The straight finisher requires `AttackStraightV2_Double`, not the left
  uppercut's collider or damage value.
- The two-hand charge clip carries
  `app.SequenceTrackObject.CH9PlayerGauntletChargeLevel` markers. Reading those
  tracks reached charge level 2 in campaign. However, replacing the raw clip also
  left `PlayerMelee.actionID` unknown; animation and charge markers alone do not
  prove working attack/cancel dispatch.
- A test hook on `CommandUpdater.isRequested` can give false confidence: native
  melee code may fetch the command group and read `IsRequested` directly. A queued
  request observed in an updater hook was absent at melee consumption time. Keep
  diagnostic command injection separate from normal-input acceptance evidence.

The standalone `dlc_gauntlet.lua` lab adapter was subsequently tested after a
process restart, without the EMV resource helper or live collider repairs:

- Right and left mouse-click punches both reached the campaign Molded's native
  `DamageController.addDamageCore`: 300 raw damage, 108 applied in that contact.
  The finisher delivered 500 raw / 300 applied; the level-2 charge delivered
  3,000 raw / 1,800 applied. These values are observations, not fixed campaign
  damage multipliers. Only incoming player damage was blocked for the test.
- The motion name is not authoritative for collision selection. Clip 2443 is
  named `Knuckle_Attack-R-Uppercut`, but emits request 45,
  `AttackBodyblowR_Double`, rather than request 44, `AttackUppercutR_Double`.
  The exported request and runtime marker must agree. The adapter now checks
  exact source IDs (43, 40, 45, 42; charged attacks 32/33), not any nonnegative ID.
- Native `MotionDelegateTagManager.addTagFromMotion` loses the melee action tag
  after a raw clip replacement. A narrowly scoped post-hook adds the native
  `MeleeAimIdle` tag, with `PlayerMelee` as sender, for the two owned charge clips
  in the corresponding aim FSM states. This restored attack/cancel dispatch.
  No tag-list pointer writes or global action-ID overrides are needed.
- Native `SequenceTrackObject.CH9PlayerGauntletChargeLevel` markers reached
  level 2. `Melee.AimAttackC` selects missing campaign clip 2407, which the
  adapter replaces with 2690 or 2691. Both attacks returned to idle. Charge
  tests used bounded diagnostic native command requests, not held-button input;
  normal held-RMB acceptance remains a separate test.
- Switching to Knife during charging restored both original Ethan hand meshes
  and bank type 30. Re-equipping reused exactly three owned dynamic banks.
  Pausing inside an active punch cleared all registered attack colliders;
  resuming completed the animation without rearming that interrupted hit.
- `app.PauseManager` is not a managed singleton. Check the actual
  `app.GameManager.get_IsPause` and scene-loading state. The current motion task
  must also be the non-null owner task before requesting gauntlet collisions.
- `sdk.create_resource` plus `REResource:create_holder` supplies the mesh,
  material and motion-list holders directly. Balance the temporary resource
  reference on success and failure; retain holders and original hand resources.
  Verify hand ancestry and exact motion-bank identity before changing anything.
  Keep the isolated banks until their native player is destroyed, rather than
  removing a bank while native animation state may still refer to it.

The opt-in lab now exposes AMG-Dual and disarms its adapter on update failures.
Its Lua regression coverage includes ownership guards, cached resource creation,
all four punch mappings, both charge levels, finite completion, pause/external
task/death suppression and refusal to reclaim another bank's slot.

Remaining work includes normal held-button charging and feedback, combo timing,
guard/damage/event interactions, broader load/player transitions, normal
pickup/two-way storage and resource teardown.
Do not promote this candidate based only on animation or isolated damage tests.

### Cold-load gauntlet failures

A disposable manual campaign save with AMG-Dual equipped exposed startup
problems that warm re-equips had hidden:

1. The player becomes discoverable before its motion manager, fallback bank and
   hand renderers are ready. Preparation now resolves all dependencies before
   allocating resources or appending banks. Scene loading does not consume the
   bounded post-load readiness timeout. Initial equip also waits for player control.
2. Native saved equip can select `Melee.ReadyStart` before bank type 9910 exists,
   leaving an empty clip with end frame zero. After the actual idle clip becomes
   available, one normal-priority `Melee.ReadyIdle` request repairs that initial
   state. It does not force an FSM state or replace a finite equip/other action.
3. Correct mesh/material paths and non-null resource holders do not prove native
   readiness. Both hand renderers remained invisible with `MeshReady` and
   `MaterialReady` false. Waiting for Ethan's original hands to finish loading
   did not solve this, while switching away and back could hide the failure.

The successful cold-load configuration combines two hidden Transform/Mesh children
in the inventory prefab with the explicit `_Data/dlc_gauntlet_assets.txt` dependency
set. The hidden children preload Joe's hand resources; the existing Ethan hand
renderers still perform the visible swap. No Joe gameplay components are imported.
The manifest includes both hands, MDFs, streaming texture companions, record-system
textures/render targets, and GauntletW/Knuckle motion lists. Only paths are committed,
not extracted game assets.

Controlled comparison using the same save and runtime adapter:

- Hidden mesh references without the dependency copy left the item prefab unready
  and blocked loading. Adding unused resource-table paths also stalled; this did
  **not** establish that unused table entries themselves were the cause.
- Copying the dependencies with the original componentless prefab allowed loading
  but still produced invisible, unready hand renderers.
- Hidden native mesh references plus the dependency copy loaded the saved equipped
  gauntlets visibly, with both mesh/material readiness flags true and no manual
  re-equip. The initial empty-motion recovery reached idle automatically.

The exact native reason runtime-created holders fail on first binding has not been
isolated. Do not replace this evidence with arbitrary delays or per-frame rebinding.
An existing save loading after a preload fix is also evidence against prematurely
diagnosing the earlier loading-screen stall as save corruption.

### Single-gauntlet variants

AMG-78a (`CH9_WP000`, WeaponID 61) is `wp1610_GauntletR`, not `wp1600`.
AMG-78 (`CH9_WP001`, WeaponID 62) is `wp1600_Gauntlet`. They share Joe's
hand resources with Dual, but native `CH9PlayerMeshController` uses gauntlet
parts 2/3 only on the left; the right hand uses bare part 0. Dual uses parts
2/3 on both hands. Each variant now has an isolated bank type (9911/9912,
with 9910 retained for Dual), followed by Knuckle and Ethan's axe fallback.
Only variants with matching namespaced inventory prefabs are prepared.

An isolated native Motion/Mesh probe sampled every frame of the source clips.
It had no hit controller or collision component and did not animate the player:

| Motion | AMG-78a request | AMG-78 request |
| --- | --- | --- |
| 2441, right hook (Knuckle) | 16 | 16 |
| 2641, left hook | 37 | 34 |
| 2443, right uppercut (Knuckle) | 17 | 17 |
| 2643, left straight finisher | 39 | 36 |
| 2662, weak charge release | 23 | 20 |
| 2663, medium charge release | 24 | 21 |
| 2664, full charge release | 25 | 22 |

Do not reuse Dual's request 45 for Knuckle motion 2443: the single-gauntlet
clip really requests `AttackUppercutR` (17). Both charge-start clips (2660,
239 frames) expose level-1 and level-2 markers; the loop (2661, 229 frames)
retains the reached level. Releases use 60/130/130-frame clips. Some track
samples also report zero or -1; damage must still require the expected attack
request, not just any successful track read. The runtime track-index cache
includes variant bank type because motion IDs repeat across the source lists.

The exported RCOLs preserve source damage/stun data. AMG-78a's three charge
strengths are 150/300/500 damage with 100/200/300 stun; AMG-78's are
300/700/2000 damage. These are raw source values, before campaign difficulty,
hit location, passive skills, and other damage modifiers.

Clean-process campaign tests withdrew both variants through the physical item-box
UI and equipped them through ordinary `Inventory.equipWeapon` requests. Both used
the expected bare-right/gauntlet-left masks, with all nine family banks retained.
AMG-78's normal mouse punch produced 100 raw / 50 added damage on an Em4000;
its full charge produced 2000 raw / 1000 calculated damage and killed that target.
AMG-78a's four normal mouse punches produced raw 100/150/150/450 and added
50/75/75/225. Its three charge strengths produced raw 150/300/500 and added
75/150/250 on another Em4000. Charge tests used bounded native command requests,
not a physical held-button test. Both returned to native melee idle without a
forced FSM reset. These results do not certify pickup/save/player transitions.

### Guard reduction

The source `CH9Weapon1600.AddGuardDamageCutRate` is 0.05 for AMG-78a/AMG-78
and 0.15 for Dual. Native `CH9PlayerDamageController.get_guardDamageCutRateCh9`
adds this to the base controller's computed reduction and clamps to [0, 1].
The adapter mirrors that in Ethan's `get_guardDamageCutRate`, after native/passive
calculation. It never writes `GuardDamageCutRate` or invents a separate damage path.
The receiver, live equipped instance, native ID, active Ethan session, and exact
adapter namespace must all match. Disabled/failed adapters and unequipped or
destroyed instances return the original result.

Live getter checks gave 0.80 for the prototype, 0.90 for Dual, and 0.75 after
switching to the campaign Knife, with the stored base remaining 0.75 throughout.
Bounded guard commands reached finite `Melee.GuardStart` (2305, 40 frames) and
`Melee.Guard` (2300, 180 frames), then returned normally. The attempted incoming-hit
comparison had no live target contact and is not evidence of guarded health loss.
Lua tests cover both bonuses, caps, receiver/weapon/namespace/lifetime exclusions,
and no duplicate hook installation.

### Gauntlet storage teardown and empty transitions

Native item-box storage destroys the weapon and its `RequestSetCollider` before
the next Lua update. A non-null managed wrapper is not a live component, and even
`get_GameObject` throws after native destruction. Cleanup checks `get_Valid` on
each component independently, clears any surviving collider/weapon trigger, and
still restores Ethan's original hand meshes, materials, and part masks. Reset is
idempotent and retains the banks owned by the live player.

After teardown, storage could leave `DownWeapon` or `Hands.ReadyStart` with end
frame zero, motion ID `0xFFFFFFFF`, and `PlayerHands.actionID == 0`. The native
lowering delegate sets action ID 13; it was not running in this failure. Menus and
the genuine down-weapon request were already closed/false, but ordinary inventory
equip requests remained pending. Adding Ethan's Hands list as a fourth fallback
bank did not fix it and is not part of the adapter.

The adapter now arms recovery only after its own tracked weapon is removed. With
the player unarmed, alive, in control, and outside menus/loading/pause, it confirms
the same empty transition across updates and makes one normal-priority
`Hands.ReadyStart` request after selecting native unarmed bank zero. The active
window is bounded; finite clips, executing native actions, other states/weapons,
external tasks, death, and loading cancel it. This is not a general FSM watchdog.

A clean-process test exercised native UI storage, automatic hand restoration,
ordinary `Inventory.equipWeapon("Knife")`, native UI withdrawal, and ordinary
re-equip of AMG-Dual with both renderers ready and three banks retained. A normal
mouse punch was also exercised after withdrawal. This verifies that round trip,
not campaign-wide pickup/player-transition coverage. Lua regression tests cover
independent component destruction and the recovery exclusions above.

### Eight-weapon campaign integration

The production `BioRand7/dlc_weapons` feature owns the gauntlet adapter; the
manual lab runner is not required. Generation adds the three gauntlet item and
weapon definitions, melee starting selections, random pickups, inspection
resources, and namespaced attack RCOLs. All seven attack records per variant
participate in ordinary damage/stun randomization without rewriting Joe's source
player RCOL. Gauntlet inventory prefabs must **not** receive
`WeaponMotionController`: their skeleton and motion belong to the player adapter,
not a weapon-local Motion component.

Recorded production-path checks:

- A full opt-in seed generated with all 151 dependencies. Its starting selections
  included AMG-Dual for Ethan, AMG-78a for Mia, and Spirit Blade for Clancy VHS.
  This was an output inspection, not a complete deployed-seed playthrough.
- Native world pickups made from the production gauntlet templates added AMG-78a
  and AMG-78 to Ethan's inventory. Both used their imported IDs, correct display
  models, and ordinary equip. AMG-78's examine view loaded its silver model.
- The actual debug grant added four missing weapons and skipped the four already
  owned. A second request added zero and reported all eight already owned.
- A disposable manual save containing all eight weapons survived a process
  restart: four remained in inventory, four in storage. Saved AMG-78 equipped
  automatically into bank 9912 with finite 284-frame ReadyIdle clips, valid hand
  resources, accepted input, and no adapter failure. Earlier isolated-lab tests
  established the Dual's storage and equipped cold-load paths separately.
- A native chapter transition from Ethan to ship VHS Mia (`Pl2000`, chapter 13)
  created nine correctly owned banks. All three gauntlets were withdrawn through
  her physical item box and equipped normally. A mouse click started Dual motion
  2441. Bounded native input requests exercised full-charge Dual motion 2691 /
  request 5 and single-gauntlet motion 2664 / request 6, including active collision
  windows and return to idle. Switching to MiaKnife restored `pl2010.mesh` and
  `pl2020.mesh`. This does not establish Mia enemy damage or held-button input.

Campaign player scenes for `Pl0000`, `Pl0000_Chapter1`, `Pl2000`, `Pl2100`, and
`Pl3000` use `pl0000.motbank` and its joint map. The adapter accepts those explicit
identities only. It resolves the real `PlayerMeshController.RArmMesh` and
`LArmMesh` TDB fields, then verifies ancestry belongs to the active player. Global
`findGameObject("Pl0000HandL")` is not valid for Mia/VHS or overlapping chapter
loads. Left/right selection is explicit, not inferred from Ethan object names.
Exact fallback bank ID/type, hand readiness, and ownership are checked before
allocating dynamic banks. Broader player-transition/combat acceptance remains
separate from source-scene compatibility and mocked regression coverage.

The weapon-local attachment now uses `Quaternion.identity()`. Live REFramework
showed `Quaternion.new(0, 0, 0, 1)` produces `(x=0,y=0,z=1,w=0)`, a 180-degree
rotation, because the constructor takes **w, x, y, z**. The corrected cold-loaded
weapon has `(x=0,y=0,z=0,w=1)`. Earlier hits still landed with the old value, so
this is a verified transform correction, not evidence that it caused every
collision or animation problem. Full equip/reset regression tests now exercise
the transform, hidden skeleton, both hand masks, and all three variants.

### CH8 throwable backend research

The 2026-10-10 campaign experiment isolated `via.Transform` and
`app.CH8ShellManager` from `CH8_SystemObject` into a new prefab. Its native awake
path populated 30 bullets and six shells for each of the three grenade types.
The campaign already called this owned manager's `updateList`; adding a second
Lua update loop was unnecessary. Pools are `ShellManager.ManageList<T>`, not
ordinary lists: inspect `List`, `UsedCount`, and `UnusedCount`.

A native `createThrowable` test with Ethan as owner established:

- The last float is the remaining fuse, despite the `ElapsedTimer` field name.
  Zero exploded on the next update; `3.0` exploded after approximately 3 seconds.
- Native `doUpdate`, `doLateUpdate`, explosion, and deactivation ran. The regular
  grenade returned to six unused / zero used slots about 5.2 seconds after the
  explosion, without forcing its completion or resetting the pool.
- Read flight from `RigidBody.getPosition(bodyId)` and `getLinearVelocity`, not
  just the GameObject transform. The latter remained at launch until explosion.
- An unchanged source grenade RCOL damaged campaign `Em4000`: raw 1,500,
  calculated/applied 750, health 2,629.4749 to 1,879.4749. Enemy health was not
  edited. This proves one ordinary-grenade native hit, not every target/filter
  or incendiary/neuro-stun behavior.

Three source inventory prefabs were subsequently registered through a separate
research-only `ItemSettings` file. `createItemInstance` initialized the native
`CH8WeaponThrowable` with Ethan's Inventory and Item. `onStartStandby`,
`onPinPulled`, and `onStartThrow(player, false)` ran; the latter allocated a shell
with the source grenade's two-second fuse. These were direct diagnostic calls,
not normal input, stock-consumption, pickup, save/load, or campaign certification.
At that research stage the three grenades remained outside the campaign pool.
The later dedicated adapter and production tests below supersede that boundary;
these direct calls alone were not sufficient for promotion.

The shared `pl1000_Grenade.motlist` contains ready 2000/2001, walk/jog 2020/2021,
guard 2300/2305/2306, throws 2400/2401, and standby 8001/8002. The separate
Grenadebomb/Thermatebomb/Stangrenadebomb lists each supply hand-pose clip 8000;
copying only the shared list misses these dependencies. Hidden-skeleton sampling
observed `CH8PlayerPinPulledTrack.IsPinPulled` near frame 20 of standby 8001,
and `CH8PlayerThrowTrack.IsThrow` near frames 29/30 of overhand/underhand clips.
Consume the actual tracks rather than using wall-clock release delays.

Subsequent **normal mouse-click** research tests completed pin-pull, throw,
native shell allocation, one-item consumption, and return to the campaign melee
idle for all three types. These tests still used isolated research registrations,
not the supported campaign export. Additional findings:

- Wait for the native attack motion ID (2400/2401/2407) after the FSM enters its
  attack state before replacing it with standby 8001. Replacing it on the FSM
  name change alone lets the next native update overwrite the standby clip.
- Use `MotionNodeCtrl.getSequenceTracks(UInt32, Tracks, Single, Single)` over
  the elapsed frame range, with once-only pin/release flags. Current-frame-only
  sampling can miss short event tracks when frames are skipped. An explicit
  range test found the pin event in 19..22 but not 0..19 or 22..30.
- Source `CH8PlayerThrowable` permits standby-to-throw cancellation after frame
  23, with underthrow thresholds -10 degrees standing and 0 degrees crouching.
  Native `onStartThrow(player, under)` still handles release position, collision
  adjustment, velocity, and remaining fuse.
- A bounded diagnostic held-command override reached standby loop 8002 and
  native `isForceThrow` at elapsed 2.501 seconds for the three-second incendiary
  fuse. It then released and consumed the last item. This proves the native
  cooking path through a command override, not a physical held-button test.
- `Inventory.reduceItem(id, 1, false)` can destroy the equipped weapon on the
  final item immediately at release. Do not retain/use its native component
  for the rest of the throw. The prototype retained only the clip ID, waited
  for its end, updated the target bank, and requested `Hands.ReadyStart`.
  Subsequent ordinary knife equip worked. Production recovery still needs
  bounded, same-player/task guards and interruption coverage.

Unmodified source shell collisions also worked against normal campaign
`Em4000`. Incendiary explosions recorded raw 500 / applied 250, followed by
additional native blast and fire damage. Neuro-stun recorded raw 500 / applied
250 on a fresh target and set native `EnemyActionController.isSlippingAcid`;
the target then lost health over time. This is native acid/status compatibility,
not proof of identical Not a Hero balance or stun duration. A separate one-HP
target died from the neuro-stun explosion. None of these tests wrote enemy HP.
Keep shell damage and status behavior distinct when deciding campaign balance.

Local evidence is retained under `.analysis/dlc-weapons-2026-10-10/`:
`grenade-mouse-throw-691.json`, `grenade-incendiary-native-hit-721.json`,
`grenade-stun-native-hit-725.json`, `grenade-stun-acid-status-731.json`, and
`grenade-cooking-last-item-736.json`. These ignored traces are research artifacts,
not packaged assets or a substitute for regression tests.

The follow-up implementation now has a separate deterministic
`DlcGrenadeWeapons.ExportShellManager` exporter and `dlc_grenade_pool.lua` /
`dlc_grenade.lua` runtime modules. The explicit lab export/runner now includes
them; the later eleven-weapon integration also wires them into the campaign catalog
and automatic runtime startup. The exporter isolates all three shell
prefabs and their RCOLs under the supplied BioRand namespace, preserves the
native default-bullet pool, and copies no other `CH8_SystemObject` components.
The 80-path dependency manifest is text only; no extracted assets are committed.

A cold campaign process exposed a missing sound dependency that the earlier warm
research session had hidden. All three registered grenade PFBs reported
`Exist=true`, `Standby=true`, but `Ready=false` and `Valid=false`; a one-shot
standby/path request did not fix them. Fresh handles for both an unchanged source
PFB and the adapted PFB failed identically. Adding the 22 referenced sound
containers, event lists, rigidbody list, and banks and restarting made all three
registered PFBs ready and valid immediately after loading, without a repair
request. This validates the dependency set as a group, not each file as an
individual cause. Sound dependencies can block complete prefab readiness, not
merely silence playback. A closure walker restricted to `CH8/` and `CH9/` silently
misses their `Sound/` references. Keep the shared interaction-exception sound bank
as well as the three weapon/explosion banks in the export manifest.

Live standalone-module validation created the exported manager, parented it to
Ethan, observed all native pools ready, destroyed it through the native GameObject
API, observed the singleton become null, and recreated it successfully. Cleanup
is deferred to UpdateBehavior and refuses to destroy an owned object while a
different manager occupies the singleton. Script reset without a scene reload
is not certified; do not adopt an arbitrary pre-existing manager by name.

The new controller completed a normal-click last-item neuro-stun throw and then
an ordinary knife equip. It owns nine separate banks (9950..9952), samples source
pin/release tracks over frame ranges, and gates throws on pool capacity and
motion readiness. Live `getMotionInfo` returned hand pose 8000 = 10 frames,
standby 8001 = 50, standby loop 8002 = 180, idle 2001 = 284, and both throws = 55.
Mock tests additionally cover cooking/forced release, missed-event failure,
pause, task loss, death, weapon removal, infinite stock, last-item destruction,
bank ownership, and bounded recovery. These are not live save/load,
pickup/storage, other-player, or interruption certification. Cancellation before
release currently keeps the item; exact CH8 damage-interruption behavior remains
unverified. Production activation must wait for the remaining inventory/export
integration and lifecycle tests.

Two further interop pitfalls were reproduced:

- A fresh `sdk.create_instance("via.Prefab", true)` with its own path and standby
  request loaded the fixture; duplicating an existing Prefab returned an invalid
  managed pointer in that test. This does not establish the cause of every earlier
  readiness failure.
- Use `ValueType.new(sdk.find_type_definition("via.Ray"))` for a native by-value
  ray. A boxed `sdk.create_instance("via.Ray", true)` could read back plausible
  fields but launched in the wrong direction. Replacing it with the value-type
  container produced the requested forward velocity. See the
  [REFramework value-type API](https://cursey.github.io/reframework-book/api/types/ValueType.html).

Other blocked families have distinct requirements:

- CH8 grenades use standby, pin-pull, timed throw, underthrow, stock consumption,
  collision adjustment, and `CH8ShellManager` grenade pools.
- CH9 throwing knives and spears derive from `CH9WeaponThrowable`, with their
  own equip/throw/use-type lifecycle. They are not conventional `WeaponGun`s.
- Spirit Blade has `RequestSetCollider`, CH9 owner status/order, attack-hit checks,
  and recovery values. A knife mesh swap would omit its behavior.
- Stake Bomb also has `CH9WeaponLiquidBombAppend`; a campaign remote-bomb ID is
  not automatically an equivalent implementation.

### End of Zoe projectile investigation

The October 10 follow-up separated CH9 inventory weapons from their native pool
objects. `CH9Weapon1500` (Throwing Knife, ID 64) and `CH9Weapon1800` (Throwing
Spear, ID 65) derive from `CH9WeaponThrowable`. Their inventory instances have
`UseType = Equip`, while the projectile prefabs contain a disabled
`CH9WeaponThrowable` with `UseType = Throwing` and `IsInventoryWeapon = false`.
Preserve that distinction; enabling the projectile's weapon component or replacing
it with a conventional gun is not an established repair.

`CH9_SystemObject` in `ch9/scenes/chapter/chapter9.scn.20` owns
`app.CH9ShellManager`. Its native initialization requests these pools:

| Manager field | Source prefab under `CH9/Prefab/Weapon/` | Count |
| --- | --- | --- |
| `ThrowingWp1500Prefab` | `NailKnifeBulletS.pfb` | 2 |
| `ThrowingWp1800Prefab` | `HarpoonBulletS.pfb` | 10 |
| `LiquidBombPrefab` | `JoeLiquidbomb.pfb` | 5 |
| `ThrowingWp0000Prefab` | `KnuckleBulletS.pfb` | 2 |

These counts were first recovered from native initialization and subsequently
observed in a fresh campaign process using the isolated research export. The
explicit `DlcCh9Projectiles.ExportShellManager` research exporter
retains only Transform and CH9ShellManager from the source object, preserves all
four pool slots, and clones every projectile and attack RCOL under the requested
namespace. It does not load CH9 gameplay/save managers, register inventory weapons,
or run during normal generation. The exported spear and bomb retain their native
`CH9InteractWeapon` recovery children. Nine files are produced deterministically;
missing attack resources fail before any writes. No extracted assets are committed.

Read-only live method-address discovery plus disassembly of the matching installed
executable established these contracts:

- `CH9PlayerGun.throwWeapon(bool)` obtains the player camera's shoot ray, performs
  launch/collision calculations, and calls `CH9ShellManager.setupThrowingWeapon`
  with the owning player GameObject: pool type 4 for knife, 5 for spear. It then
  calls the projectile's virtual `activate(position, rotation)`. Its false branch
  includes random spread, but both normal and aimed source attack callbacks pass
  **true**. Do not infer that normal input should use the random-spread branch.
  A raw camera-position spawn is not exact parity.
- `CH9ThrowingWeaponBase.setup` marks the slot used, binds the owner to the
  projectile and its weapon, calls `HitController.setAttackOwner`, assigns the
  shell ID/type, and enables its request-set collider. That base setup does not
  cast the owner to a CH9 player interface.
- Spear setup additionally resolves shared `IPlayerOrder` and
  `Command.CommandUpdater`. This is encouraging for campaign ownership, but does
  not prove its later collision, attachment, or recovery paths work on Ethan.
- The knife and spear use projectile-specific collision files
  `NailKnifeBulletS.rcol` / `HarpoonBulletS.rcol`, not their inventory `wp1500` /
  `wp1800` RCOLs. Damage changes must target the actual projectile data.
- `CH9WeaponThrowable.reduceWeapon()` uses its bound `Inventory`, checks native
  infinity state, converts WeaponID to the item name, and calls
  `Inventory.reduceItem(..., 1, true)`. Launch and stock consumption are separate
  operations; a projectile appearing alone is not a working inventory lifecycle.
- Stake Bomb remains distinct: `CH9WeaponLiquidBombAppend.isUsable(owner)` checks
  the camera ray and placement geometry. `use(owner)` computes placement and
  selects the Transform/Joint overload of `CH9ShellManager.createBomb`. The source
  also carries water/sink settings. Do not replace it with a remote-bomb alias or
  bypass the placement check.

Local discovery found 86 candidate dependencies, including CH9 motion lists,
projectile/interaction prefabs, effects, textures, and sound resources outside
`CH9/`. This remains a research list, not a certified cold-load manifest. Next
validation must prove native startup/readiness, campaign-owned launch and damage,
stock loss exactly once, spear/bomb recovery, interrupted throws, last-item
cleanup, storage, and cold save/load before promoting the three inventory items.

#### Campaign knife probe and native throw contract

A later fresh-process research deployment loaded the isolated manager, three
inventory fixtures, and the candidate dependency closure without loading CH9
gameplay managers. Native pools initialized to 2/10/5/2. Direct projectile probes
bound Ethan as owner; the knife collided and returned to its pool, while the spear
lodged in a wall with its shared player-order and command-updater references set.
Neither observation alone proves inventory integration or recovery.

A player-scoped prototype then registered the research item settings and attached
Joe's knife/spear motion banks, with the campaign axe bank as fallback. The native
`CH9Weapon1500` inventory object was withdrawn through the real item-box menu and
equipped on Ethan. Keeping its native class while routing only its weapon category
and player motion bank allowed both arms to reach Joe's ready and throw motions.
The research item had an icon but blank text: messages still require integration.

Important contracts established by disassembly and the live trace:

- Top-level motions 2400/2405 are blend trees; release tracks are on children
  2406/2407. Matching raw node ID to the top-level clip loses the event.
- `app.SequenceTrackObject.CH9PlayerActionTrack.IsWepThrowTiming` has a knife
  window at frames 27..38 (78-frame clip), and a spear window at 32..40
  (90-frame clip). These are boolean windows, not two throws. The source callback
  latches release once, hides/disables the equipped object with
  `Util.setActive(go, false, false)`, and calls native `reduceWeapon()` once.
  Restore the held object on completion/cancellation when it remains valid.
- `CH9WeaponThrowable` does not have the CH8 throwable's `Item` field. Read stock
  through the bound inventory; preserve native `reduceWeapon()` and infinity
  handling instead of mutating stack fields directly.
- Source launch starts at shoot-ray origin plus direction times 0.1 for the knife
  or 1.0 for the spear. It casts two 1 m rays: `DamageCheckDefault`, ignoring the
  owner, then `EffectCheckBullet`. Both disable backface hits. A first hit backs
  off by 1.0/0.2 m (knife/spear); the second replaces that result only when its raw
  hit position is nearer than the first adjusted start, backing off by 1.0/0.36 m.
  Preserve this unusual comparison rather than silently simplifying it.
- The raw Lua `castRay` signature uses `app.Collision.CollisionSystem.HitResult`
  without an ampersand, despite C# exposing `ref`. The probe passed a retained
  HitResult through an owned 8-byte reference cell and checked that native code
  had not replaced it. A missing Lua overload can silently return nil; assert the
  method and distinguish nil from a real false/no-hit result.
- Use `ShellManager.makeShellID()` when the shared manager is available. The
  initial pool-only probes used zero and are not the production ID policy.

Three normal mouse-click knife throws released once each at approximately frame
27, consumed stock 3 -> 2 -> 1 -> 0, and produced native shell IDs 1/2/3. Two hit
ordinary `Em4000`: `DamageController.addDamageCore` identified `NailKnifeBulletS`,
raw damage 200, and applied 100 then 60 according to the native hit calculation.
The last knife disappeared and G17 could subsequently equip; the empty-hands FSM
showed `Hands.ReadyStart`, so do not call the empty-hands presentation certified.
Player damage protection was enabled for this disposable-save test, not enemy
health or projectile damage overrides. Evidence is under the ignored
`.analysis/dlc-weapons-2026-10-10/ch9-knife-*` and `ch9-last-knife-*` captures.

This is **research evidence, not supported-item promotion**. Aimed/held input,
interruption and low-FPS timing, near-wall hit branches, exact launch orientation,
spear recovery/damage, stake-bomb placement, messages, generation, and cold
save/load still need validation. The prototype currently uses the camera rotation;
the source constructs its orientation from direction/up vectors. No CH9 player
component was grafted onto Ethan. Spear recovery was subsequently proven in the
scoped experiments below; complete campaign integration remains open.

### Spear recovery and stationary pool roots

The research spear released at the native frame-32 marker, consumed one item,
and lodged in a campaign wall. Two independent issues then prevented a complete
throw/recovery cycle:

- **Transform parenting is not just lifetime ownership.** Native projectile roots
  are children of `CH9ShellManager`'s GameObject. Parenting that manager to Ethan
  made a lodged spear and its recovery child move by exactly the player's warp
  delta. A 0.75-metre approach did not reduce their separation. Detaching only
  the owned manager while preserving its world transform fixed this. Keep the
  manager stationary and explicitly destroy it on reset/player replacement.
  This observation is specific to the CH9 spear; the existing CH8 grenade root
  also uses player parenting, but its physics behavior needs an independent test
  before claiming the same gameplay defect.
- **Successful inventory insertion is not completed pickup.** Native
  `CH9InteractWeapon.getWeapon` entered `insertInventry`, added one spear, and
  hid the projectile, but never reached `successInteract`. Its `equipWeapon`
  branch for WeaponID 65 unconditionally opens
  `CH9TutorialManager.openTutorial(HarpoonChange)` (enum 15). Campaign has no
  tutorial manager, so this aborts after inventory mutation but before the
  success delegate deactivates the shell. WeaponID 64 similarly opens
  `knife_Desc` (enum 13). Neither branch actually equips the throwable.

Do not force `doStart`, manually run the success callback, or tick the manager
again to hide this symptom. `SuccessInteractHandler` was already registered.
Bounded hooks observed native `CH9ShellManager.updateList` and `updateThrowable`
running every frame. The missing transition was the pickup completion callback,
not pool scheduling. Native pointer identity must use `get_address`, not the
Lua userdata wrapper's `tostring` output.

`BioRand7/dlc_ch9_pool.lua` is a tested, currently unwired research foundation.
It requests the isolated manager prefab, waits for the native 2/10/5/2 pools,
keeps the root unparented, and defers destruction to its update callback. It
refuses to destroy its object while a foreign CH9 manager occupies the singleton.
Its recovery hook skips only the tutorial-only `equipWeapon` branch for an exact
namespaced spear returned to the current campaign inventory by an interaction
belonging to its own spear pool. It does not replace inventory insertion, force
equip, clear used flags, or import CH9 tutorial/system managers. It is not yet
wired into candidate selection or the ordinary runtime archive allowlist.

Live validation used the actual module after destroying the earlier research
manager and disabling its prototype recovery bypass. Two normal mouse throws
and native interaction-command pickups each produced stock 4 -> 3 -> 4 and pool
availability 10 -> 9 -> 10. `InteractSuccessCount` became 1, `get_isActive` and
`get_isUsed` became false, and the interaction disabled. The next throw used a
different native pool slot, so these observations prove recycling/count recovery,
not traversal and reuse of every slot. An explicit reset destroyed/recreated the
manager and restored all pool types without parenting or adapter errors. Evidence
is in the ignored `ch9-module-*` captures, with the failing/control traces in
`ch9-recovery-*`. Pickups used bounded UI-command pulses after a short physical
F-key tap was not consumed. At this stage cold-save/title transitions, full-pool
cycling, full-inventory rejection, spear damage, and aimed/held throws were unproven.
All 30 Lua suites passed under Lua 5.4, along with 23 focused DLC weapon/runtime
.NET tests; this does not promote the three CH9 throwables into generation.

The old IDA address export did not match the installed executable. Refresh method
addresses from the current TDB and normalize against the current process image
base before reading the executable from disk. Windows x64 unwind entries can be
chained fragments, and short leaf/tail-call functions can have no unwind entry at
all. A single unwind range is not necessarily the whole method. Durable evidence
should name methods and executable identity, not assume old absolute addresses.
This pass used PE timestamp `0x69c1fe87`, image size `0x9a37000`; the local IDA MCP
endpoint was unavailable, so these findings came from matching-file disassembly,
not a successful IDA decompilation.

### Knife/spear motion adapter foundation

The subsequent spear damage trace recorded a normal campaign `Em4000` hit from
`HarpoonBulletS`: raw damage 100, calculated damage 50, and the remaining 2.456905
HP removed. The enemy had already taken knife damage; this is evidence of native
campaign damage routing, not a full-health damage or balance measurement. A normal
mouse throw produced one native release and stock 4 -> 3. Player protection was
enabled for this test; enemy health was not edited. Local evidence is retained as
`ch9-spear-molded-damage-1020.json` and `ch9-spear-molded-throw-1021.json`.

Three additional pitfalls appeared while validating aimed input:

- **A successful launch does not complete the player action.** Normal throws
  returned to idle, but an aimed spear remained in `Melee.AimAttackC`, clip 2405,
  at frame 90. Restore the still-valid held object and request the campaign's
  `Melee.AimToReady` only when this owned attack reaches its actual clip end.
  Preserve other task/state/clip replacements instead of forcing a recovery
  through damage or scripted actions. A bounded five-second native-command
  probe then launched once, returned to aiming while Aim remained requested,
  and returned to ReadyIdle when released. This is not physical held-button QA.
- **Last-stock destruction happens before animation completion.** Native
  `reduceWeapon()` immediately removes the final equipped object. A later update
  that only checks the currently equipped weapon loses the attack's completion.
  Keep a bounded record of the clip/state, without the destroyed weapon, and
  request `Hands.ReadyStart` at completion if no replacement weapon/task/state
  has taken over. Aimed last-stock spear and knife checks reached empty hands
  without a stuck attack. The campaign empty-hands presentation caveat described
  for grenades still applies; do not claim its zero-length arm clip is repaired.
- **Blend-root duration is not resource readiness.** `getMotionInfo` successfully
  resolves roots 2400/2405 with end frame -1 for both weapons. Their children
  2406/2407 report 78 frames for the knife and 90 for the spear. Require the roots
  to exist, but validate positive duration on the children and idle. Requiring
  positive duration on blend roots caused a reproducible false loading timeout.

`BioRand7/dlc_ch9_throwable.lua` now implements this foundation with the separate
owned CH9 pool. It retains native concrete weapons, uses custom bank types
9960/9961, samples the child release track over a frame range, calls native
projectile activation and stock reduction, and restores visibility on completion
or interruption. Sample release before checking clip completion so a low-FPS
update crossing both boundaries cannot lose the throw. Pool exhaustion does not
consume stock. Startup hooks are deduplicated by native address: the concrete
knife and spear `doStart` methods share a body in the tested executable. Discard
MotionInfo, sequence-track, collision-result, and reference-cell scratch objects
on reset and player replacement without invoking expired objects.

The actual module was hot-adopted into the controlled campaign session, with the
old research motion hooks made inert and the existing owned banks/pool retained.
A normal mouse knife throw consumed stock 2 -> 1 and returned to ReadyIdle. An
aimed last-stock knife throw reached empty hands. An aimed spear throw/recovery
cycle produced stock 3 -> 2 -> 3 and pool availability 10 -> 9 -> 10, with the
native success callback deactivating the shell. Separately, filling the last
inventory slot with throwing knives made native spear pickup show its rejection
indicator; no spear was added, no success callback ran, and the lodged projectiles
remained active. Evidence is in the ignored `ch9-runtime-*`, `ch9-aim-*`, and
`ch9-spear-full-inventory-1053.json` captures.

All 31 Lua 5.4 suites and 23 focused DLC weapon/runtime .NET tests passed. The
new strict mocks cover both weapons and attack modes, blend-root readiness,
shared hook registration, native collision filters/reference marshalling/offsets,
low-FPS release, last-stock destruction, infinity, missing tracks, pool exhaustion,
pause/menu/task/clip interruption, and reset/player scratch invalidation. The
module at foundation commit `51592c0` was not yet selected by generation or included
in the ordinary runtime archive allowlist. Warm adoption is not fresh startup validation. Clean-process
pickup/messages/storage/save/title checks, all-pool-slot reuse, exact source
orientation parity, and Mia/Clancy coverage remained open at that stage. Stake Bomb
was still unsupported; the later Item adapter below handles its distinct placement logic.

### Knife/spear campaign integration

The subsequent production pass registers both throwables under `BioRand/DlcWeapons`
and packages their motion and pool modules through the ordinary runtime allowlist.
Both configuration gates remain required, and the feature remains default-off.
That pass brought the catalog and debug grant to 13 candidates, with Stake Bomb
still excluded. Its 88-path CH9 manifest brought the deduplicated dependencies to
319; the later Stake Bomb pass adds its resource scene for totals of 89 and 320.
Keep the full native four-kind CH9 shell-pool layout, including the unused knuckle pool.

Knife and spear are six-per-slot stack weapons, with no gun or ammo-parameter graft.
Loose pickups reuse LiquidBomb's native `InteractDetailSearch`; preserved gun
pickups retain their donor identity while rebinding the concrete CH9 weapon type.
Damage randomization edits copied projectile RCOLs, never source DLC collisions.
`Impact` uses native damage/stun 200/50 for knife and 100/100 for spear.
`AttackToWeapon` is deliberately zero/zero contact data, not a missing damage stat.
Consumable throwables must not replace the guaranteed permanent starting blade.

Adding CH9 IDs exposed a configuration naming pitfall: item IDs contain underscores,
but configuration IDs permit hyphens. Normalize `CH9_WP003` to `ch9-wp003` consistently
in definitions, static supply pools, relief tables, and Lua enemy-drop lookups.
The runtime generic-drop list also needed all five supported consumable DLC weapons,
not just new C# entries. Gate those runtime candidates on both DLC permissions and
leave the existing boss-drop whitelist unchanged. Regressions exercise actual
configuration readers rather than supplying a test-only weight delegate.

Live checks used a fresh process with the production feature only, not hot adoption
or the prototype CH9 loader. Both exact campaign prefab paths became ready, the
adapter owned its motion banks, and its native pool initialized without error.
Generated loose-pickup fixtures rendered the correct models/messages and entered
inventory through normal pickup interaction. These interactions used bounded native
UI-command pulses after physical key taps were not consumed, not direct grants.
A normal mouse knife throw consumed the last item. A bounded native Aim/Attack
probe consumed the last spear, left one lodged native projectile, and recovery
returned stock 0 -> 1 and pool availability 9 -> 10. Neither action remained stuck.
The real item-box menu stored the spear and withdrew both knife and spear; the
knife then equipped into `Melee.ReadyIdle`, clip 2001, with a positive duration.
Evidence is in the ignored `ch9-production-*` captures.

A backed-up disposable manual save held one knife equipped and one spear in
inventory. Returning to title and continuing restored both exact counts, both
campaign prefab paths, owned banks, a ready pool, and knife idle clip 2001 with
284-frame duration. A normal mouse throw then consumed the last saved knife and
completed into the same empty-hands state as the vanilla comparison. This pass
needed no prototype loader, hot adoption, or explicit adapter reset. A subsequent
cold-process load restored the same save and exposed an additional intra-session
scratch lifetime bug described below. With the managed-validity guard deployed,
another cold-process load restored both items and normal mouse throws consumed
the last spear and knife, each 1 -> 0, without adapter failure or a stuck action.

Test fixture ownership produced a separate research-only trap: a newly created
`via.Prefab` wrapper became invalid while the retained production pool prefab and
an earlier fixture holder remained valid. `duplicate()` did not cure it. Reusing
the earlier inactive fixture holder allowed the spear pickup test; this does not
justify replacing production item registration or resetting healthy native pools.

The full test project passed 614 tests and all 31 Lua 5.4 suites passed. A normal
opt-in seed (35825) generated successfully from a clean baseline augmented with
the explicit dependency manifest. Running setup against a currently modded install
can instead incorporate installed randomizer patch PAKs into a contaminated baseline.
Do not certify generation against that result or deploy it over the active seed.

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
17. **Test exported runtime packaging, not just embedded files.**
    `RandomizerOutput.REFrameworkScriptPaths` is an explicit allowlist. All three
    DLC runtime modules were embedded but missing from fresh output archives;
    a warm installation with manually copied scripts hid that defect. Both export
    formats now contain the modules. Regression tests resolve every literal
    local `require("BioRand7/...")` in packaged Lua and compare packaged bytes
    with the embedded module. Check a fresh full seed, not only a successful DLL
    build or isolated lab deployment.
18. **Disposable pickup fixtures need independent save identities.** A research
    prefab reused its SCN donor's `SaveGUID`; saved pickup state changed its
    ItemDataID and disabled it in a different player scene. Production placement
    paths already clone with new GUIDs. Do the same before serializing a research
    prefab. Do not run the SCN GUID remapper on an already serialized PFB: empty
    object GUIDs can accidentally remap nil references. Construct from the SCN
    donor, assign fresh identities, then serialize. Runtime-created prefab handles
    for newly installed fixture files also became invalid in this session; the
    cause was not established. Production inventory prefabs stayed valid, and
    native storage/withdrawal provided an independent player-adapter test.

These are the same broad lessons as Em4400: serialized component parity does not
prove native initialization, and one successful animation does not prove a
complete behavior chain. Keep observations separate from root-cause hypotheses.

## Running the isolated lab

Use a disposable save/profile and back up saves and overridden loose files first.
Installed source DLC assets are required for export. The lab copies its curated
dependency manifests, but this is not a claim of complete sound/VFX closure.
Do not redistribute extracted game assets. The export writes campaign settings
and messages, so do not assume it composes with an arbitrary generated PAK or
another loose-file override.

```powershell
dotnet run --project src/biorand-re7 -- mod -m "DLC Weapon Lab" -i "<RE7 install>" -o out/dlc-weapon-lab
```

1. Deploy the explicit lab export using the normal mod workflow, with RE7 stopped.
2. Ensure the current `BioRand7/game.lua`, `BioRand7/dlc_weapon_lab.lua`,
   `BioRand7/dlc_weapon_player.lua`, `BioRand7/dlc_gauntlet.lua`,
   `BioRand7/dlc_grenade.lua`, `BioRand7/dlc_grenade_pool.lua`,
   `BioRand7/dlc_ch9_throwable.lua`, `BioRand7/dlc_ch9_pool.lua`, and
   `BioRand7/dlc_ch9_item.lua` modules are
   deployed. Run `tools/dlc_weapon_lab.lua` manually via ScriptRunner; do not
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

### Grenade inventory startup ordering

Native quick-slot selection can expose `CH8WeaponThrowable` through
`EquipManager` before `doStart` fills its `Inventory` field. A strict immediate
owner assertion therefore disabled the adapter after a normal item-box withdrawal.
The same component later had the correct Ethan inventory without intervention.
Wait up to two active seconds for a null owner, perform no throw callbacks while
waiting, and still reject a non-null foreign owner immediately. Pause and menus
do not spend this readiness budget. Do not force `doStart` or assign `Inventory`.
The regression covers delayed ownership, wrong ownership, timeout, and reset.
A bounded one-frame UI-command pulse selected the real quick slot and reached
`Melee.ReadyIdle` with all nine banks and the owned shell pool ready; that is
native quick-slot routing evidence, not physical-key timing coverage.

### Grenade cold-save and pickup boundary

A disposable manual save held a regular grenade equipped, an incendiary grenade
in inventory, and a neuro-stun grenade in the item box. A complete process restart
restored those exact counts and locations with all three prefabs ready, nine motion
banks, an owned ready shell pool, and `Melee.ReadyIdle` clip 2001. A normal mouse
throw consumed the last regular grenade without adapter error. Quick-slot selection
then returned to G17; equipping the cold-loaded incendiary grenade also reached a
valid idle without forcing native startup. These tests used the lab namespace and
a cold-start research loader, so production campaign wiring is validated separately.

The subsequent production deployment disabled that research loader and used only
`BioRand7/dlc_weapons.lua` with both configuration flags enabled. A fresh process
loaded the same disposable save with exact grenade inventory/storage counts,
`BioRand/DlcWeapons` prefab paths, nine banks, the owned ready pool, and idle clip
2001. A normal mouse throw consumed the regular grenade without adapter failure.
A fresh LiquidBomb-derived world pickup displayed the grenade model and normal
interaction prompt, entered inventory through `InteractDetailSearch`, and equipped
with a valid idle. Pickup used a bounded native UI-command pulse after a physical
key tap was not consumed; it was not a direct inventory grant. The actual item-box
menu then stored it successfully.

After the last throw, empty hands reported `Hands.ReadyStart`, action 34, and no
arm-layer clip. The same state appeared after normal grenade storage. Pickup and
weapon switching still worked. Re-requesting that state did not change it, and a
direct `Inventory.removeEquip()` control probe threw rather than supplying a
healthy comparison. Do not infer a missing animation asset or add repeated
recovery writes from this observation alone. A subsequent native item-box menu
transfer of an equipped G17 reproduced action 34, no clip on arm layers 1/2/9,
and movement accepted. This matches the grenade-storage observation in the same
campaign setup; further empty-hands interaction/guard coverage remains required.

Loose grenade pickups use the campaign LiquidBomb template and its native
`InteractDetailSearch`. A loose item does not need a Weapon component: LiquidBomb
and MiaKnife donors have none. The registered inventory prefab supplies the native
weapon after pickup. Preserved gun pickups that do contain WeaponGun must instead
replace it with the grenade's `CH8WeaponThrowable` and rebind ItemAddTest IDs.
Do not make a test demand a component that the real donor never had.

### Stake Bomb campaign integration

Stake Bomb (`CH9_WP005`, WeaponID 66) now participates in opt-in generation and
debug grants. Unlike knife/spear it derives from `WeaponItem`, not `CH9WeaponThrowable`.
Ethan's native `PlayerItem` already binds `CH9Weapon1900` and its
`CH9WeaponLiquidBombAppend`. Keep the Item category and native `tryUse`/`use`
path; changing it to Melee would discard working placement and stock logic.

An isolated runtime prototype supplied Joe's `pl9000_LiquidBomb.motlist` under
bank type 9962 with campaign Item bank 200 as fallback, synchronized the two
Item arm layers, and repaired only the initial zero-duration ready state after
the bank became ready. Normal mouse placement played Item.Use clip 2400,
consumed one charge, and returned to ready. Invalid floor aim rejected use without
consuming stock. Native recovery returned a charge and recycled the pool slot.

Two independent callbacks assume `CH9TutorialManager` exists:

- `CH9InstallationWp1900.onSuccessInteractCh9` only closes tutorial 5. The pool
  guard skips this callback for its exact owned campaign shells. It does not
  skip base Bomb recovery or inventory transfer.
- `CH9InstallationWp1900.callSE` closes the same tutorial on explosion trigger
  `0x73CE5AE3`. A missing manager aborts `Bomb.doUpdate` after explosion damage
  registration but before advancing Rno from Wait to Explosion. The resulting
  hidden active shell can damage again on another detonation request. For owned
  campaign shells only, call the base `Bomb.callSE` directly, preserving the
  shallow-water substitution `0xE350A1FC`. Other sound triggers stay native.

Do not replace this with a global tutorial hook, forced Rno writes, or a timer
that destroys every hidden shell. Both guards check active campaign player,
exact registration path, pool ownership, absence of the DLC tutorial manager,
and reset/destruction state. Lua regressions cover those boundaries and all five
installation types. The guards are inert without a matching Stake registration.

Placed ground bombs retain the player's native command updater. `Bomb.checkExplosion`
uses its Detonate command (PC Reload binding), a damage hit, or an explicit native
request; the tested path was not a proximity trigger. A bounded native command
pulse with the audio guard recorded `JoeLiquidbomb -> Em4000`, calculated damage
1000, and health 1421.5408 -> 421.5408. A fresh normally placed bomb then killed
that target and recycled, with no direct enemy-health writes or RCOL modification.
The reused stuck shell's earlier hit is not a second successful placement test.
Physical Reload taps were not reliably consumed and still need input validation.

Local evidence: `stake-floor-place-1167.json`, `stake-recovered-1170.json`,
`stake-update-native-1190.txt`, `stake-audio-native-1190.txt`,
`stake-audio-guard-detonation-1204.json`, and `stake-fresh-kill-1209.json` under
`.analysis/dlc-weapons-2026-10-10/`. These were isolated research tests;
the capacity and production checks below supersede their registration,
pickup/storage and equipped-save gaps, not their physical-input limitation.

The final charge also follows native `PlayerItem` behavior: placement changes
stock 1 -> 0 and switches to empty hands; recovering it changes stock 0 -> 1,
restores the pool slot and clears `SetupBombCount`. Recovery does not automatically
re-equip WeaponID 66. Do not add a forced equip or stock refund to compensate.
Evidence: `stake-last-charge-1219.json` and `stake-last-recovery-1221.json`.

`dlc_ch9_item.lua` is wired into campaign and manually armed lab lifecycle
handling, with its own failure latch and explicit output packaging. It retains
the native Item category and shares the CH9 throwable adapter's pool rather than
creating a second manager. Its owned bank 9962 combines Joe's LiquidBomb motions
with campaign Item bank 200 as fallback. It waits for clips 2000, 2001 and 2400,
mirrors native Item arm work, and repairs only an initial zero-duration ready
state. Placement, stock consumption, recovery and detonation remain native.
Native `tryUse`/`use` fail closed while the owned player, inventory, motion or pool
is unavailable, including adapter failure, menus, loading and player death.
The shared pool owner's failure also disables Item use; Item failure latches
separately and never resets or updates the shared pool a second time. Session
reset clears both failure latches without reinstalling hooks.

Do not reject placement solely because the five-slot bomb pool has no unused
entry. Native `CH9ShellManager.createBomb` calls the common pool allocator, which
can recycle the oldest used entry through its callback. A fresh-process research
fixture confirmed six ordinary mouse placements consume a six-charge stack while
retaining five active shells, without error. Recovering one with empty hands
restored one charge; a bounded native Detonate pulse then cleared the remaining
four shells and returned all five slots. No forced stock, Rno or damage writes
were used. See `stake-pool-allocation-native-1190.txt` and the
`stake-capacity-*-1279/1280/1281/1283/1286/1288.json` captures.

This run initialized the new Item adapter from scratch, not by adopting the old
prototype's banks. Both arms reached valid idle; normal placement and recovery
left the test position stable. Its registration and shell tutorial guards still
used the research fixture; it did not establish cold production registration or
ordinary keyboard/controller input coverage.

A heavily probed warm session developed a test-position snap while idle, including
with vanilla G17 equipped. Both input and root-motion translation were zero in
the sampled state; transform and native warp attempts did not persist. No cause
has been established. This session must not certify movement behavior or be
saved as a new baseline; repeat with a fresh player/process. Do not introduce
recurring transform writes into the weapon adapter to mask it.

The subsequent clean production export contains 388 files, including the 320
manifest dependencies. With only production registration, Item motion handling
and tutorial guards, a fresh campaign process passed these checks:

- A generated LiquidBomb-derived pickup displayed Stake Bomb and entered inventory
  through native `InteractDetailSearch`. The pickup used a bounded UI Interact
  pulse after the automation's short key tap was not consumed, not an inventory grant.
- Native inventory equip selected ID 66 and bank 9962, with both arms in
  `Item.ReadyIdle`, clip 2001, end frame 284. An ordinary mouse placement consumed
  the final charge. Native recovery restored one charge and returned the pool slot.
- The physical item-box UI stored and withdrew that recovered bomb. A disposable,
  backed-up manual save was then written with one bomb equipped.
- A complete process restart restored stock 1, WeaponID 66, the campaign prefab
  namespace, owned ready motion banks, valid idle and all five unused shell slots.
  No prototype bank adoption, forced native startup or hot adapter reset was used.
- A normal mouse placement after cold load changed stock 1 -> 0 and SetupBombCount
  0 -> 1. A bounded native Detonate command cleared the bomb, restored all five
  unused slots and SetupBombCount 0. Every shell was inactive with update/draw off
  and Rno 2; no hidden active bomb remained. Player damage protection was test-only
  and disabled afterward. This production cleanup test had no enemy target; the
  earlier native Molded damage/kill evidence is recorded separately above.

Local captures: `stake-production-pickup-1299.json`, `stake-production-equip-1301.json`,
`stake-production-place-1304.json`, `stake-production-recover-1306.json`,
`stake-production-store-1309.json`, and `stake-production-cold-*-1315/1319/1322/1323.json`
under `.analysis/dlc-weapons-2026-10-10/`. The temporary research loader was not part
of the export. These checks do not certify physical Reload/controller input,
every installation surface/enemy attachment, placed-bomb persistence across saves,
all preserved-FSM pickups, Mia/Clancy, or chapter transitions. Save migration and
full-playthrough certification remain out of scope for this experimental adapter.

### Motion scratch lifetime on reload

Returning to title and reloading a grenade-equipped save exposed a stale cached
`via.motion.MotionInfo` in `Grenade:motion_ready`. The motion component and active
player were valid, but `sdk.is_managed_object` rejected the scratch object's old
address. Its surviving Lua wrapper and earlier `add_ref()` did not prove native
validity. The failure disabled the adapter, leaving a zero-duration `Melee.ReadyStart`
and blocking movement/equip until the scratch object was discarded and the adapter
reset. Do not misdiagnose this as failed pool initialization or missing animation
assets. Discard motion scratch data on reset and player replacement without calling
methods on the expired object. The gauntlet adapter uses the same scratch type and
now discards it on reset too. Lua regressions cover both invalidation boundaries.

A fresh process with this fix loaded the grenade-bearing save, returned to title,
and loaded it again without hot-patching or explicit adapter resets. Both loads
reached idle clip 2001 with nine banks and an owned ready pool; a subsequent normal
mouse throw consumed exactly the remaining grenade without adapter failure.

Reset boundaries alone are insufficient. A cold load with knife equipped cached
valid scratch, but equipping the spear in the same player session later passed
expired scratch to `Throwable:motion_ready`. A scoped traceback and validity
probe identified that exact `MotionInfo` address as invalid while the weapon,
shell manager, and owned pool remained valid. The resulting `sol_lua_push: ...
is not a managed object` error disabled the adapter. A successful retry or title
reload did not disprove the intermittent lifetime bug.

All three motion adapters now check `sdk.is_managed_object` before reusing scratch
and recreate it when invalid, without invoking methods on the expired wrapper.
Lua tests cover invalidation within an unchanged player session. A fresh process
with this guard replaced the cached scratch between knife and spear readiness,
then completed ordinary mouse throws of both weapons, consuming exactly one of
each. The pool stayed owned and ready; no prototype adoption, hot reset, or
behavior patch was needed. Local evidence is in `ch9-scratchfix-*` captures.
The solution build, 43 focused .NET tests, and all 31 Lua 5.4 suites passed.

An earlier heavily probed session crashed on title teardown in Wwise's
`CAkRegisteredObj` destructor (executable RVA `0x5A0E520`). The dump/log are retained
locally. This happened before any CH9 research fixtures were deployed; neither a
CH9 regression nor a causal connection to MotionInfo has been established. Keep
the audio teardown crash separate from the reproducible stale-scratch Lua error.

### Campaign flow scope

`Game:chapter()` returns `GameFlowFsmManager.GameFlowKindEnum`, not a chapter number
or `GameManager.ChapterNo`. Values 0 through 13 cover C00, C01, the C03/C04 main
flows, and the campaign found-footage scenes. For example 8 is C04_2, 13 is FF050,
18 is Not a Hero, and 21 is End of Zoe. An apparently conservative 1..5 check
silently excludes late-campaign and VHS players. The grenade adapter now accepts
0..13 while retaining exact active-player names and campaign prefab namespaces.
Tests cover every campaign flow and reject DLC/none flows; these scope tests do
not replace live Mia/Clancy motion, pool-lifetime, or interaction validation.

Automated coverage includes the 14 source identities, rejected unsupported
adapters, default-off behavior, both permission gates, manifest extraction and
missing-resource preflight, deterministic lab export, serialized native identities,
pickup interaction rebinding, messages, ammo mapping, capacity changes, and the
WeaponID 13 alias. Lua coverage checks exact namespace guards, deferred bounded
loading, failures without repeated mutations, and load/new-game invalidation.

The latest campaign integration full test-project run passed 615 tests, and all 33 Lua suites
passed under Lua 5.4. The machine's `lua` command was Lua 5.1, so a compatible
Lua 5.4 runtime was used. Automated success does not remove the manual risks above.
An opt-in full seed (35825) also generated successfully from a clean baseline
augmented with the 231 required installed resources, including grenade drop tables
and packaged runtime modules. That output was inspected, not used to certify a
complete randomized playthrough.

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
5. Finish gauntlet player-transition, held-button input, sound, and visual checks.
   Knife and spear now use their dedicated experimental CH9 adapter; broaden
   player-transition, all-pool-slot reuse, and source-orientation parity coverage.
   Stake Bomb now joins the opt-in pool after placement, recovery, native detonation,
   storage and equipped cold-save checks; broaden physical-input, installation-surface,
   enemy-attachment, placed-bomb persistence and player-transition coverage.
   Broaden CH8 grenade player-transition coverage too.
