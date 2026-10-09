# Technical Notes

I've gathered this information while working on the randomizer.

## Important directories (main game -> re_chunk_000.pak)

- (key) item locations: re_chunk_000/natives/stm/leveldesign/itemset

- prefabs: re_chunk_000/natives/stm/prefab/item

- item combine data
  - re_chunk_000/natives/stm/prefab/item/dictionarycombinedata.user.2
  - re_chunk_000/natives/stm/prefab/item/itemcombinedata.user.2

- key item prefabs
  - re_chunk_000/natives/stm/prefab/item/keyitemsettings.user.2

- message text and markup
  - re_chunk_000/natives/stm/message/*.msg.17
  - See `docs/items/MessageMarkup.MD` for RE7 message tags such as `<COLOR RRGGBB>`, `<SIZE n>`, `<ICON NAME>`, `<PAGE>`, `<TIME ...>`, and engine-supported but unobserved tag types.

## Key Item Location Randomizer

- Main House starts reuse the native Chapter 1 `app.AddItem` action in `0_Ch1Item_ItemBoxIn_FFS`, replacing the transfer from the skipped chapter. Keep the original action UID and `ForceFsmSave`/gimmick controls so the configured loadout initializes once and survives reload.
- Placed weapon chests use the template's unlocked root FSM, with its interaction references rebound to the cloned drawer. Strip the imported LockLv1 children; their state checks reference locks absent from the placed chest. Keep drawer ownership of the hidden weapon until opening, and give the weapon an identity local transform. Reusing the chest's world transform applies the placement offset twice.

- The key item location randomizer is intentionally conservative. If an item is tied to a boss reward, cutscene, or puzzle state rather than a plain inventory pickup, keep it vanilla until the full FSM sequence is understood and runtime-tested.
  - D-Series Arm, D-Series Head, Snake Key, and E-Necrotoxin retain their native rewards and story transitions. Their phase floors alone do not prove relocation safe. Do not remove the cultivation-room doors to compensate for an early E-Necrotoxin pickup.
  - Spider lockers and the deputy-body rating variants are not valid key carriers. The former need damage resources; the latter are mutually exclusive regional objects despite having no CSV difficulty restriction.
  - Key carriers also require a reviewed GUID and scene in `KeyItemCarrierSafety`; extra pickups must retain their reviewed coordinates. The Main Hall cubby is excluded because its position intersects solid geometry. Adding CSV rows must not silently expand the progression pool. Update the independent pickup prerequisites in `KeyItemProgressionAuditTests` when approving a new carrier.
- `RandomizerConfigurationDefinition.cs` is the source of truth for the `random-key-item-locations` option. Do not only edit profile JSON when changing the option.
- Do not emulate trigger-driven room-entry flags as pickup side effects. Stone Statuette (`SilhouettePazzlePieceOldHouse`) remains randomizable, but its former `c03_3_Main_EnterMiaCapturedRoom` pickup side effect is intentionally skipped because the original writer is `150_Main_EnterMiaCapturedRoom`, a physical `TriggerInAction` in `levelfsm_c03_3.scn.20`. The randomizer adds an equivalent fallback trigger on the natural Old House shadow-puzzle path so players do not need to detour through the vanilla Stone Statuette room just to advance hidden progression.
- Current unsafe preserved items include:
  - Hatch Key (`FloorDoorKey`): relocation breaks Jack's Main House progression triggers. Preserve the native pickup and its interaction/flag sequence; even reachable pre-hatch supply locations are unsafe replacements. Its original pickup must not carry another randomized key either.
  - Red Dog Head (`3CrestKeyC`): its `c03_2_Main_GetCrestInFreezerRoom` flag is the Jack 2/freezer reward progression state, not just an item-acquired flag.
  - Candle (`Candle_Lighted`): its `c03_4_Main_PazzleRoom_CandleOn` flag is part of the Lucas birthday puzzle state machine.
  - Lantern, Ox Statuette, keycards, Chapter 1 key items, Chapter 4 fuse, and complete serum remain vanilla until exact access-sphere and FSM side effects are proven safe.
- The D-Series Arm is a real progression precondition for the Lucas/snake-room branch: `objectivefsm_c03` reads `c03_3_Main_GetEvlineArm`, and the attic ladder branch also depends on the resulting Lucas card-key flow. Keep the graph edge into snake-key rooms gated on both Snake Key and D-Series Arm.
- Route reachability is not sufficient for pickups that advance a level FSM. Their rules also carry an earliest-safe phase, which is checked independently of carrier masks. Crank cannot be collected before completing the Old House Stone Statuette puzzle, Crow Key cannot be collected before the crank bridges, and Power Cable cannot be collected before the Corrosive-gated ship rooms. If no carrier satisfies both the phase floor and the normal route deadline, preserve the vanilla pickup.
- Flashback copies of key items are separate scripted state. Do not replace them with filler and do not use them when deriving relocated pickup side effects.
- Scripted flashback scenes (`ff*` and chapter 4 `*past.scn` scenes) are protected from enemy replacement, enemy multiplication, extra enemy placement, and collision-bearing extra objects. Loose non-key supplies may still be randomized.
- Carrier safety is an invariant over every eligible placement, not only generated routes. Extend `KeyItemLocations_AllProgressionFlagCarriersRespectMinimumSafePhases` whenever another pickup is shown to advance an FSM phase.
- Chapter 3 key matching prefers less occupied rooms, treating the kitchen, pantry, and crawlspace as one starting area. Keep this a soft preference with seeded tie-breaking: constrained keys may share an area, and every assignment must still pass the existing carrier, phase, and complete-route checks.
- The `4-key-hints` output asset is a seed-specific offline HTML map spoiler. `KeyItemMapRepository` maps stable pickup GUIDs to room-label centroids on embedded floor plans under `_Data/key_item_maps`; marker numbers and legends retain route-sphere order, original carrier names, and collapsible scene/GUID/world-position diagnostics. Keep `KeyItemLocations_RandomizesSupportedKeyItemsIntoRouteSafeNormalPlacements` covering every eligible route target so a newly added pickup cannot silently lose its map marker.
- Chapter 4 `ff050/bf` itemset metadata identifies Ship S2 but not an exact room. Those spoiler markers are deliberately labeled as approximate floor-level locations. Do not invent exact room calibration without additional structured evidence.

## DLC PAK file table

The .pak files are found in the `dlc` directory.

| **PAK file**        | **DLC**                 | **Comment**                                  | **URL**                                                                 |
|---------------------|-------------------------|----------------------------------------------|-------------------------------------------------------------------------|
| re_dlc_stm_529930   | Banned Footage Vol. 1   | Nightmare, Bedroom, Ethan Must Die           | https://store.steampowered.com/app/529930/Banned_Footage_Vol1/         |
| re_dlc_stm_530610   | Banned Footage Vol. 2   | 21, Daughters, Jack's 55th Birthday          | https://store.steampowered.com/app/530610/Banned_Footage_Vol2/         |
| re_dlc_stm_530611   | End of Zoe              |                                              | https://store.steampowered.com/app/530611/End_of_Zoe/                  |
| re_dlc_stm_564190   | Not a Hero              |                                              | https://store.steampowered.com/app/564190/Not_A_Hero/                  |
| re_dlc_stm_530595   | Defense Coin            |                                              | https://store.steampowered.com/app/530595/Defense_Coin__Madhouse_Mode_Unlock/ |
| re_dlc_stm_530596   | Attack Coin             |                                              | https://store.steampowered.com/app/530596/Attack_Coin__Madhouse_Mode_Unlock/  |
| re_dlc_stm_530597   | Instinct Coin           |                                              | https://store.steampowered.com/app/530597/Instinct_Coin__Madhouse_Mode_Unlock/ |
| re_dlc_stm_530598   | Reload Coin             |                                              | https://store.steampowered.com/app/530598/Reload_Coin__Madhouse_Mode_Unlock/   |
| re_dlc_stm_530599   | Universal Coin          |                                              | https://store.steampowered.com/app/530599/Universal_Coin__Madhouse_Mode_Unlock/|

## Internal DLC IDs

`
(chapter|c)0?7_1 => Bedroom
((chapter|c)0?7_2)|cardgame|survival => 21
(chapter|c)0?7_3 => Nightmare
(chapter|c)0?7_4 => Daughters
(chapter|c|ch)0?8 => NotAHero
(chapter|c|ch)0?9 => EndOfZoe
birthday => Jacks55thBirthday
/(e|i)md => EthanMustDie
(dlcitem|coin)_01 => DefenseCoinAndMadhouseUnlock
(dlcitem|coin)_02 => AttackCoinAndMadhouseUnlock
(dlcitem|coin)_03 => InstinctCoinAndMadhouseUnlock
(dlcitem|coin)_04 => ReloadCoinAndMadhouseUnlock
(dlcitem|coin)_05 => UniversalCoinAndMadhouseUnlock
`

## DLC integration notes

General lessons:

- The C# TDB in `.analysis/re7_src` is the fastest source for class names, inheritance, fields, and enums. Use IDA for xrefs, decompilation, strings, and runtime control flow after narrowing the target.
- `CH8` is Not a Hero and `CH9` is End of Zoe. Jack's 55th Birthday logic uses `Birthday*` and `FsmBirthday*` classes, not CH8 classes.
- Bringing a DLC object into the main game usually requires the full runtime component chain and asset load chain. A prefab, mesh, or enum ID alone is rarely enough.
- `natives/stm/scenes/master.scn.20` and `natives/stm/scenes/dlc/dlc_active_root.scn.20` are important DLC scene roots. Standby/default-standby state on the DLC folder chain can decide whether referenced DLC scenes and assets are actually mounted.
- If a DLC asset still does not appear after scene data looks correct, compare what the DLC mode loads from the main menu rather than only inspecting the final object prefab.
- Live REFramework MCP is configured through `C:\Users\Julian\.codex\config.toml` and runs `F:\RE_Modding\re-engine-mcp-master\mcp-server` against `http://localhost:8899`.
- Confirmed live MCP probes on 2026-05-06: `reframework_get_tdb` returned 78687 types, 507723 methods, 147762 fields, and 103892 properties; `reframework_get_camera` returned FOV 81, near clip 0.05, far clip 1000; `reframework_get_singletons` exposed relevant managers including `app.DLCContentManager`, `app.DLCContentSceneManager`, `app.EnemyGeneratorManager`, `app.InventoryManager`, `app.ItemManager`, `app.ItemResourceManager`, `app.ObjectManager`, `via.ResourceManager`, and `via.SceneManager`.
- `reframework_get_inventory` is a thin MCP wrapper over `GET /api/inventory` and still returned a generic invocation error after retrying in a loaded gameplay state. Treat that endpoint as needing WebAPI/plugin investigation before using it for RE7 inventory research.

Birthday skills:

- Skills are defined under `natives/stm/prefab/item/birthdayskillitemsetting.user.2` and use IDs `skl001` through `skl023`.
- The `skl###no` entries are locked/dummy Birthday variants and should not be injected as usable main-game skills.
- Direct placement of `Prefab/Skill/skl001/Skl001PassiveSkill.user` into Ethan's `app.PlayerPassiveSkillManager.PlayerPassiveSkill` worked and granted infinite ammo, proving the main-game player manager can consume Birthday passive userdata.
- For inventory support, the important chain is the original skill inventory prefab `Prefab/Skill/skl###/Skl###.pfb`, its `app.PassiveSkillItem`, the referenced `PlayerPassiveSkill.user`, messages copied into `ui_item_mes.msg.17`, and REFramework handling to avoid the native Birthday-only inventory callback freeze.
- Birthday skills do not have normal world pickup meshes. Static/random placements should use a known-good pickup shell and override `app.Item.ItemDataID` to the chosen `skl###`.

Not a Hero and End of Zoe enemies:

- DLC enemies need imported enemy templates and spawn-info templates in `template.scn.20`; missing templates should be skipped rather than emitted partially.
- Not a Hero generator replacements require `app.CH8EnemyGenerator`, `app.CH8EnemyPool`, `app.CH8EnemySpawnInfo`, and the concrete `app.CH8EnemySpawnInfoOptionEm####`.
- End of Zoe generator replacements use `app.CH9EnemySpawnInfo` and `app.CH9EnemySpawnInfoOptionEm####`; no `CH9EnemyGenerator`/`CH9EnemyPool` types were found in the current TDB.
- `app.EnemySpawnInfoOptionDLC` exists, but real DLC spawn-info objects do not universally include or require it.
- Template import must match concrete `UnitAlias` and prefab alias, not just option class suffix. Some CH8 aliases reuse base stacks.
- `Em4210` = Fat Headless Molded. It uses `UnitAlias = Em4210`, but the runtime stack is `app.CH8Em4200*` plus `app.CH8EnemySpawnInfoOptionEm4200`.
- `Em4400` = Mama Mold.
- `Em4450` = Little Crawler.
- `Em4460` = Mama Mold. A wrapper object with nested `Em4450` exists, but `main300generator.scn.20` also has a normal generator-style `UnitAlias = Em4460` source.
- `Em4500` = Mutated Lucas. This is the Not a Hero final boss and should stay opt-in/special-case until runtime-proven.
- `Em4600` = Fumer. It uses `UnitAlias = Em4600`, but the runtime stack is `app.CH8Em4000*` plus `app.CH8EnemySpawnInfoOptionEm4000`.

IDA MCP confirmation pass, 2026-08-25, RE7 `1.0.0.7`, SHA-256 `D550260029B51A580E206B9A12A4EB7F4A1B4DD230BBA5DEFCCA81DA40597321`:

Full function addresses, control-flow evidence, signatures, and implementation consequences are recorded in [DLCIntegrationEvidence.MD](DLCIntegrationEvidence.MD).

- The missing CH8 command actions are not caused by an explicit chapter check in `app.CH8Em4400ActionController::doAwake149958` (`0x1418DDB00`). Its first call is `app.CH8MoldedActionController::doAwake144706`; if that base call leaves a managed exception, the Em4400 method returns before inserting any actions.
- `app.CH8MoldedActionController::doAwake144706` (`0x141E35C20`) and `app.CH8EnemyActionController::doAwake150859` (`0x1410EF950`) both require the `app.CH8GameRankManager` singleton when the enemy has an initialized `EnemyRankManager`. A missing singleton takes the null/error path before the derived action insertion block.
- This dependency covers the known ordinary CH8 families: Em4000, Em4200, and Em4400 call the CH8 molded base; Em4450 calls the CH8 enemy base directly. The preferred experiment is now a narrow campaign-only rank-sync bypass/fallback, not manual action construction.
- Base `app.EnemySpawnInfo::setupInstance90108` already gets the current rank from the ordinary `app.GameManager` and applies it with `EnemyRankManager.setRank`. Skipping only the earlier CH8-specific rank synchronization should therefore retain campaign rank semantics.
- `app.CH8EnemyGenerator::.ctor323011` (`0x1401066A0`) sets `IsEnablePause = true`; imported CH8 generators must be explicitly unpaused. `app.CH8EnemyPool::.ctor323010` is only a thunk to the base pool constructor.
- IDA contains no `CH9EnemyGenerator` or `CH9EnemyPool` functions. `app.CH9EnemySpawnInfo::.ctor325718` (`0x14032F790`) sets one default value and calls the base spawn-info constructor, supporting base generator/pool use for CH9.
- CH8 need-area requests are unsuitable for campaign FSMs: `requestSpawnWithNeedArea189521` only queues a deduplicated Spawn operation when the spawn-info GameObject has `UpdateSelf`; `update189520` drains it through `requestOperationWithNeedArea189526`, which calls the CH8 blackboard and generator manager.

Not a Hero and End of Zoe items:

- The [DLC weapon investigation](weapons/dlc_weapons.md) records all 14 weapon identities, the opt-in four-weapon lab, runtime failures, and the player-system requirements for AMG-Dual. No weapon from that catalog is campaign-certified or enabled in normal pools.

- `app.ItemManager::createItemInstance319138` (`0x142146680`) returns null when `ItemData` or `ItemPrefab` is missing, otherwise instantiates the prefab at the owner transform and parents it to the owner.
- `app.fsm.ItemAddTest::start357511` (`0x14045C1B0`) tests its own `_Multiple`, `_ItemList`, and `_ItemDataID` path. Changing only the scene `app.Item.ItemDataID` is not sufficient for these interactions.
- `app.CH9PassiveSkillItem::onInsertInventory113604` (`0x1415621D0`) requires `app.ICH9PlayerOrder` from the inventory owner and registers attack/guard amulets and the passive skill through that interface. Ethan cannot consume this callback unchanged.
- `app.CH9Weapon1600::onInsertInventory328503` (`0x140556DD0`) resolves `app.CH9PlayerEquipManager` from the inventory owner and calls `registerKnuckleWeapon`; Joe's gauntlets are a player-system port, not a data-only weapon import.
- `app.CH8WeaponRemedyAppend::use324610` (`0x140AA29F0`) requires a CH8-compatible player interface and optionally updates `CH8GameRankManager` remedy-use score. `app.CH9WeaponRemedyAppend::use328562` (`0x1414BDDC0`) likewise checks a CH9-specific player interface. Campaign healing should use a campaign-native remedy adapter.

## Recipes

- `prefab/item/dictionarycombinedata.user.2`
  - Defines what recipes are shown in the combine menu
  - Only contains ItemDataID keys for a `Dictionary` that is evaluated at runtime
  - All entries MUST be valid item IDs (see enum `app.ItemID`)! Otherwise the game does not load (infinite loop). Own IDs are not allowed.
  - Must reference recipe `ResultItemID` values from `prefab/item/itemcombinedata.user.2`, not recipe `DataID` labels. The local RT TDB (`reframework/il2cpp_dump_rt.json.gz`) declares `app.ItemCombineData.getData(System.String)` with parameter `resultItemID`; the exact-table recipe implementation likewise builds dictionary entries from result IDs. Multiple recipe variants share one visible result entry.
  - If there are more than 8 entries the additional ones won't be accessible in the combine menu, only the icons can be seen but the cursor can't be moved to them. Modding it via REFramework is possible but not ideal
  - Duplicate entries lead to weird GUI behaviour

- `prefab/item/itemcombinedata.user.2`
  - DataID is referenced by the dictionary above
  - source item IDs and result item ID must be valid
  - the combine GUI has very strict validations, random combinations do _not_ appear!
  - items _can_ be combined with themselves
  - EnableFlag refers to a flag which allows the item to be crafted. E.g. the flag `eab670f7-4475-4258-b900-cbe91664c9a3` which is `cmb_enable_DybbukMedicine` activates the ability to craft the serum
  - IsTrophyTarget: Likely refers to the achievement "1st Place at the Science Fair" where you must create all items that contain Chem Fluid and Strong Chem Fluid
  - IsTutorialTarget: ?

Original recipes:

```text
  Original crafting recipes
      1x Burner Grip                    +   1x Burner Nozzle                  ⇒   1x Burner                        
      1x Herb                           +   1x Chem Fluid                     ⇒   1x First Aid Med                 
      1x Herb                           +   1x Strong Chem Fluid              ⇒   1x Strong First Aid Med          
      1x Supplements                    +   1x Chem Fluid                     ⇒   1x Psychostimulants              
      1x Solid Fuel                     +   1x Strong Chem Fluid              ⇒   2x Flame Rounds                  
      1x Supplements                    +   1x Strong Chem Fluid              ⇒   2x Neuro Rounds                  
      1x Solid Fuel                     +   1x Chem Fluid                     ⇒ 100x Burner Fuel                   
      1x First Aid Med                  +   1x Separating Agent               ⇒   1x Chem Fluid                    
      1x Psychostimulants               +   1x Separating Agent               ⇒   1x Chem Fluid                    
    100x Burner Fuel                    +   1x Separating Agent               ⇒   1x Chem Fluid                    
      1x Strong First Aid Med           +   1x Separating Agent               ⇒   1x Strong Chem Fluid             
      1x Flame Rounds                   +   1x Separating Agent               ⇒   1x Strong Chem Fluid             
      1x Neuro Rounds                   +   1x Separating Agent               ⇒   1x Strong Chem Fluid             
      1x Gunpowder                      +   1x Strong Chem Fluid              ⇒  10x Enhanced Handgun Ammo                                             
      1x Repair Kit                     +   1x Broken Handgun                 ⇒   1x M19 Handgun                   
      1x Repair Kit                     +   1x Broken Shotgun                 ⇒   1x M21 Shotgun                   
      1x Gunpowder                      +   1x Chem Fluid                     ⇒  10x Handgun Ammo                  
     10x Handgun Ammo                   +   1x Separating Agent               ⇒   1x Chem Fluid                    
     10x Enhanced Handgun Ammo          +   1x Separating Agent               ⇒   1x Strong Chem Fluid             
     10x Handgun Ammo                   +   1x Gunpowder                      ⇒   5x Enhanced Handgun Ammo    
```

Recipes in the combine GUI:

```text
      1x Herb                                               [Drug]     +   1x Chem Fluid                                         [Material] ⇒   1x First Aid Med                                      [Drug]
      1x Gunpowder                                          [Material] +   1x Chem Fluid                                         [Material] ⇒  10x Handgun Ammo                                       [Shell]
      1x Solid Fuel                                         [Material] +   1x Chem Fluid                                         [Material] ⇒ 100x Burner Fuel                                        [Shell]
      1x Supplements                                        [Material] +   1x Chem Fluid                                         [Material] ⇒   1x Psychostimulants                                   [Drug]
      1x Herb                                               [Drug]     +   1x Strong Chem Fluid                                  [Material] ⇒   1x Strong First Aid Med                               [Drug]
      1x Gunpowder                                          [Material] +   1x Strong Chem Fluid                                  [Material] ⇒  10x Enhanced Handgun Ammo                              [Shell]
      1x Solid Fuel                                         [Material] +   1x Strong Chem Fluid                                  [Material] ⇒   2x Flame Rounds                                       [Shell]
      1x Supplements                                        [Material] +   1x Strong Chem Fluid                                  [Material] ⇒   2x Neuro Rounds                                       [Shell]

```

Ideas for new recipes:

```cs
        // Ammo -> Ammo
        CreateRecipe("HandgunBulletL", 20, "ChemicalL", 1, "MagnumBullet", 10), // Enhanced Handgun Ammo + Strong Chem Fluid => 44 MAG Ammo
        CreateRecipe("ShotgunBullet", 10, "ChemicalM", 1, "MagnumBullet", 5), // Shotgun Shell + Strong Chem Fluid => 44 MAG Ammo
        CreateRecipe("HandgunBulletL", 10, "MagnumBullet", 5, "ShotgunBullet", 20), // Enhanced Handgun Ammo + 44 MAG Ammo => Shotgun Shells
        CreateRecipe("BurnerBullet", 100, "HandgunBulletL", 10, "FlameBulletS", 5), // Burner Fuel + Enhanced Handgun Ammo => Flame Rounds
        CreateRecipe("HandgunBulletL", 10, "ChemicalS", 1, "AcidBulletS", 5), // Enhanced Handgun Ammo + Strong Chem Fluid => Neuro Rounds

        // Heal -> Heal
        CreateRecipe("RemedyM", 1, "RemedyM", 1, "RemedyL", 1), // First Aid Med + First Aid Med => Strong First Aid Med
        CreateRecipe("RemedyM", 1, "Herb", 1, "RemedyL", 1), // First Aid Med + Herb => Strong First Aid Med
        CreateRecipe("RemedyL", 1, "RemedyL", 1, "Stimulant", 1), // Strong First Aid Med + Strong First Aid Med => Steroids
        CreateRecipe("RemedyM", 1, "EyeDrops", 1, "Depressant", 1), // First Aid Med + Psychostimulants => Stabilizer
        CreateRecipe("EyeDrops", 1, "Herb", 1, "Depressant", 1), // Psychostimulants + Herb => Stabilizer
```

## Inventory

Ethan starting inventory: natives/stm/leveldesign/fsm/chapter1/other -> tested successfully with chainsaw

Mia starting inventory (VHS): natives/stm/leveldesign/fsm/chapter4/chapter4_1/other/4-1startinventory.user.2

Mia starting inventory (w/ machine gun): natives/stm/leveldesign/fsm/ff050/other/ff050_startinventory.user.2

Clancy VHS starting inventory natives/stm/leveldesign/fsm/ff000/other/startinventory_ff000.user.2

---------

Files where inventory size is changed:

- levelfsm_c07_3_normalmode.scn.20
- levelfsm_c07_3_survivalmode.scn.20
- stage1_seta.scn.20
- stage1_setb.scn.20
- stage2_seta.scn.20
- stage2_setb.scn.20
- stage3_seta.scn.20
- stage3_setb.scn.20
- ship1f.scn.20
- shipoutside.scn.20
- mainhouse_east.scn.20
- oldhouse.scn.20
- levelfsm_ff000.scn.20
- level_fsm_ff050.scn.20
- levelfsm_c07_2_storymode.scn.20
- levelfsm_c04_1.scn.20
- levelfsm_c04_2.scn.20
- mainflow_gimmick.scn.20
- levelfsm_c09_4.scn.20
- ch8_game.scn.20

## Enemies

### IDs

- em2000: Mia
- em3000: Jack
- em3001: Jack (Nightmare DLC)
- em3002: Jack (Daughters DLC)
- em3100: Marguerite
- em3101: Marguerite (Bedroom DLC)
- em3600: Marguerite Fight Boss
- em4000: Molded
- em4100: 4-Legged Molded
- em4200: Fat Molded
- em5400: Insects
- em5500: Darker Insects
- em55[10..40]: Insects
- em6400: end of zoe jack
- em8000: Jack Saw Fight
- em8001: Jack with Shears (Nightmare DLC)
- em8100: Jack Eyes Boss
- em8900: Evie Final Form

### IMPORTANT WHEN RANDOMIZING

- Enemy placement directives (`preserve`, `aggro`, `nodup`, `cull`, `Include`, and `Exclude`) are consumed from the enemy spreadsheet and embedded CSV. See `docs/enemies/enemy_spawning.md` for precedence, definition-ID wildcards, and remaining native encounter safeguards.

- in the FSM, the UIDs are **not** arbitrary. they are actually state IDs hard-coded in .fsm.16 files!
- therefore, only assign valid UIDs in biorand
- wandering enemies must be edited by removing the MapName in via.navigation.NavigationSurface

### General notes

- Initial Jack (Em3000) encounter: natives/stm/leveldesign/fsm/chapter1/amaro.scn.20
- Molded Group outside of Swamp Hut: natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20

## Player Stats

- HP: natives/stm/prefab/character/pl0000/pl0000maxhealthtable.user.2

## Misc

- Eye drop params (max time and range): natives/stm/prefab/system/systemparameterdata.user
- Lucas puzzle door FSM: leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20
