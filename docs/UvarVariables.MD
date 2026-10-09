# Uvar Variables

Refers to RE7's Uvar variables in `natives/stm/userdata/globalvariables.uvar.2`. These play an important role for the game's internal state.

**Adding spawn groups?** These variables are not accepted by the current `SpawnGroups.State` field. Use the [spawn-group authoring guide](enemies/spawn_groups.md) and its [FSM state reference](enemies/spawn_group_states.md) for exact supported trigger values. A Uvar GUID is not an FSM owner GUID, and integer counters such as `c03_1_Main` are not state names.

## FSM / Scene Usage Notes

Findings below come from the IDA-backed REFramework type database plus vanilla `LevelDesign/Fsm/**/*.scn.20` and `LevelDesign/Flag/**/*.user.2` scene data.

- Rows marked `Likely` are direct scene-data observations from this scan. They identify where vanilla FSMs write, read, or gate a GUID, but the wording is intentionally conservative until manually verified.
- `via.fsm.action.SetBool` writes a UVAR by `Guid`; `Status` is `False = 0`, `True = 1`. Vanilla main-flow scene objects are the best evidence for when a story flag is set.
- Treat the GUID as authoritative. The variable name string embedded in an FSM action can drift from the `globalvariables.uvar.2` name that owns the same GUID!
- `app.fsm.IntSet` and `app.fsm.IntTest` operate on integer UVARs through their `IntFlag` GUID. `IntTest.CompareType` is `Equal = 0`, `LessThan = 1`, `GreaterThan = 2`.
- `via.fsm.ConditionUserDataExpression` reads a UVAR by `Expression` GUID. `ExpressionReferenceType` is `LocalUserData = 0`, `GlobalUserData = 1`, `Direct = 2`. For global flags in `globalvariables.uvar.2`, use `GlobalUserData`.
- `ConditionNodeWorkEnd`, `ConditionAllActionNodeWorkEnd`, and their `Failed` variants are FSM action-state transition conditions, not UVAR reads. They are useful for understanding graph flow, but they do not identify a global flag by themselves.
- `app.GameFlowFlagPkg.UseFlags` lists the UVAR GUIDs required by a game-flow node. These `leveldesign/flag/*.user.2` files usually gate the next numbered flow folder, so the filename can be one step after the `SetBool` object that wrote the flag. Example: `c03_5_main_500.user.2` gates on `c03_5_Main_JackGrabEventEnd`, while `c03_5_main_600.user.2` gates on `c03_5_Main_MukadeJackBattleEnd`.

### Chapter / mode scope

The semantic labels below use RE7's story and DLC structure, plus the vanilla FSM folder names:

| Prefix | Scope used in notes |
| --- | --- |
| `c01` | Guest house prologue and Mia fights |
| `ff000` | Sewer Gators / Derelict House Footage videotape |
| `c03_1` | Main house opening, dinner escape, and garage Jack fight |
| `c03_2` | Main hall, processing area, dog heads/crests, and Jack 2 chainsaw fight |
| `ff030` | Mia Old House videotape |
| `c03_3` | Old House, Marguerite, lantern, D-series arm, and snake key handoff |
| `c03_4` | Testing Area, Lucas, barn fight, and party-room puzzle |
| `ff040` | Happy Birthday videotape |
| `c03_5` | Boat house finale, Jack 3, Mia/Zoe choice |
| `c04_1` | Wrecked Ship as Mia, including Eveline flashbacks and old videotape setup |
| `ff050` | Wrecked Ship old videotape |
| `c04_2` | Post-ship rescue, Ethan handoff, and salt-mine approach |
| `c04_3` | Salt mine, final Eveline fight, and ending flow |
| `c03_IMD` | Ethan Must Die DLC |
| `c07_1` | Bedroom DLC |
| `c07_2` | 21 DLC |
| `c07_3` | Nightmare DLC |
| `c07_4` | Daughters DLC |

## List of variables

### ProgressiveNumber

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 642f63ef-e64e-4582-9817-b8ab77440752 | c00_Main | Int32 | 0 |  |  |  |
| Unknown | dc69294c-9c26-4134-aea1-4e0d55a2e3d0 | c01_Main | Int32 | 0 |  |  |  |
| Unknown | 12efcf13-f603-4851-8f56-2ea91bb32506 | ff000_Main | Int32 | 0 |  |  |  |
| Unknown | 5866012e-1770-4ab6-bf7e-0cde466c31d5 | c123_Main | Int32 | 0 |  |  |  |
| Unknown | 3162ea0e-fda1-45fb-90df-1a5e452aae66 | c03_1_Main | Int32 | 0 |  |  |  |
| Unknown | 142bd8c2-507b-42c4-9937-5843f44a9215 | c03_2_Main | Int32 | 0 |  |  |  |
| Unknown | 962a0588-2cd4-4bdf-aa67-f8ad41a72401 | ff030_Main | Int32 | 0 |  |  |  |
| Unknown | 48d5c953-042b-4eec-83df-797020b4c3e8 | c03_3_Main | Int32 | 0 |  |  |  |
| Unknown | 1ee1ab13-8add-432e-b551-4622d86dce50 | c03_4_Main | Int32 | 0 |  |  |  |
| Unknown | 6401b0c9-b440-440f-be6a-01428d5d1316 | c03_4A_Main | Int32 | 0 |  |  |  |
| Unknown | 431c41fa-e3d9-4b01-9916-cf4d6babd9bb | c03_4B_Main | Int32 | 0 |  |  |  |
| Unknown | 2acaf03b-8b8a-46f7-8d8b-5da2f85efd24 | c03_4C_Main | Int32 | 0 |  |  |  |
| Unknown | 05231673-26e3-4198-87bc-3b4dd7e5b601 | ff040_Main | Int32 | 0 |  |  |  |
| Unknown | 08824973-37a1-4d8b-b532-48c982a576fc | c03_5_Main | Int32 | 0 |  |  |  |
| Unknown | a8b78583-869b-4dbf-893a-f02e9fef5141 | c324_Main | Int32 | 0 |  |  |  |
| Unknown | dd7b06ae-a534-453d-b3e4-110ade395b8d | c04_1_Main | Int32 | 0 |  |  |  |
| Unknown | 1eb74f11-6b0b-4768-8568-fa4128029916 | ff050_Main | Int32 | 0 |  |  |  |
| Unknown | 3388d97d-1a5e-471c-bb8b-c5e429100e8c | c04_2_Main | Int32 | 0 |  |  |  |
| Unknown | 8896dc9d-e47e-4ccc-8db9-4301073f61fd | c04_3_Main | Int32 | 0 |  |  |  |

### c01_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 717183bb-e1a0-4432-8566-a41f50a77035 | c01_Main_EndMovie | Boolean | False | Guest house prologue flow: marks that the opening movie/cutscene has ended. | Written by `0_EndMovie` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_EndMovie true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_25.user.2` |
| Likely | 430f5340-0828-429c-b6be-33b2e2ad8e7e | c01_Main_GetDown_Car | Boolean | False | Guest house prologue flow: marks that Ethan got out of the car at the guest-house approach. | Written by `25_End_CarEvent` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_GetDown_Car true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_50.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_GetDown_Car (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | c3d467ad-5f46-4221-bef4-45238c75b792 | c01_Main_BackdoorLock | Boolean | False | Guest house prologue flow: marks the guest-house back-door lock setup after the approach. | Written by `50_sm0192_BackEntrancedoorLock` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_BackdoorLock true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_100.user.2` |
| Likely | 50bbdc8c-a77f-45ec-a1df-8ed2ada2b263 | c01_Main_FallB1Floor | Boolean | False | Guest house prologue flow: marks that Ethan fell through to the guest house B1 floor. | Written by `100_FallB1Floor` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_FallB1Floor true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_200.user.2` |
| Likely | 51a56997-2628-4a73-908e-41a1c6f4df7e | c01_Main_MeetMia | Boolean | False | Guest house prologue flow: marks the first reunion with Mia in the guest house. | Written by `200_MeetMia` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_MeetMia true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_300.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_MeetMia (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 9459667e-829a-4cca-abce-cb52186003d3 | c01_Main_EnterB1SafeRoom | Boolean | False | Guest house prologue flow: marks entry into the guest-house B1 save room. | Written by `300_EnterB1SafeRoom` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_EnterB1SafeRoom true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_400.user.2` |
| Likely | a56ecf39-cd07-4016-950d-8ea31b1ac3c7 | c01_Main_WalkIntonextRoom | Boolean | False | Guest house prologue flow: marks moving from the save room toward Mia's cell area. | Written by `400_WalkIntoNextRoom` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_WalkIntonextRoom true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_500.user.2` |
| Likely | f545848d-66f2-4b1f-bd63-ee10df9e9722 | c01_Main_ScreamVoiceAroundWashRoom | Boolean | False | Guest house prologue flow: marks the scream/voice cue near the wash room. | Written by `500_ScreamVoiceAroundWashRoom` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_600_00 true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_600.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_ScreamVoiceAroundWashRoom (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 197760ea-7245-4d32-9d4d-cf72309bd14d | c01_Main_FirstBattle | Boolean | False | Guest house prologue flow: marks the first Mia boss phase in the guest house. | Written by `600_FirstBattle` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_FirstBattle true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_700.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_FirstBattle (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 1aa5f552-940f-4615-999f-3a94c058ff7d | c01_Main_MoveOutsideBathroom | Boolean | False | Guest house prologue flow: marks moving out of the bathroom area after the first Mia phase. | Written by `700_MoveOutsideBathroom_1stPlay` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_MoveOutsideBathroom true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_800.user.2` |
| Likely | 10c6c685-bef0-4f50-b1f1-ae13b47d7d6d | c01_Main_SecondBattle | Boolean | False | Guest house prologue flow: marks the second Mia encounter and phone-call transition. | Written by `800_SecondBattleTelephoneCalling` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_SecondBattle true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_900.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_SecondBattle (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | c59676c1-5718-438c-bfb5-49650a9f065d | c01_Main_TelephoneCall_Transition | Boolean | False | Guest house prologue flow: marks the phone-call transition before the attic route. | Written by `900_sm0113_Ck_PhoneFlowEnd` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_TelephoneCall_Transition true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_1000.user.2` |
| Likely | f7486e9b-8924-494c-9738-c26fa3c2e055 | c01_Main_FuseGet | Boolean | False | Guest house prologue flow: marks obtaining the fuse in the guest house. | Written by `1000_Fuse_Get` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_FuseGet true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_1050.user.2` |
| Likely | c2a7b288-cb79-40c3-bea1-b476349ec28b | c01_Main_FuseBox01AInFuse | Boolean | False | Guest house prologue flow: marks inserting the fuse into the guest-house fuse box. | Written by `1050_sm0245_FuseBox01AInFuse` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_FuseBox01AInFuse true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_1100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_FuseBox01AInFuse (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 45a607cf-6911-4160-86a6-d7d2d7f622fc | c01_Main_ThirdBattle | Boolean | False | Guest house prologue flow: marks the third Mia boss phase. | Written by `1100_ThirdBattle` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_ThirdBattle true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_1200.user.2` |
| Likely | c88a6f30-f154-482f-83a2-2e8b63c0d8a6 | c01_Main_LastBattle | Boolean | False | Guest house prologue flow: marks the final attic Mia boss phase. | Written by `1200_Fsm_LastBattle` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_LastBattle true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_1300.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_LastBattle (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 20930bf7-d87a-43ec-9bac-75b7838a3d80 | c01_Main_FinishLastBattle | Boolean | False | Guest house prologue flow: marks that the final guest-house Mia battle is over. | Written by `1300_FinishLastBattle` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_FinishLastBattle true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_1400.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_FinishLastBattle (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 1c077e6d-d8ab-4318-9d3c-2452e5aa3399 | c01_Main_End | Boolean | False | Guest house prologue flow: marks the end of the guest-house prologue flow. | Written by `1400_c01e07_00End` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_Main_End true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c01_main_end.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_Main_End (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Unknown | 1c440e53-905b-4d3f-bc02-1d0bacf61fae | c01_Main_TitleDispEnd | Boolean | False |  |  |  |
| Unknown | bbe1f744-a5a0-40ed-a42f-b1a2eb2c4f81 | c01_Main_GetFFVideo | Boolean | False |  |  |  |
| Unknown | a49d7070-6205-4f41-bb6f-9914ec23e960 | c01_Main_CompleteFF | Boolean | False |  |  |  |
| Unknown | 8f835418-93a0-4bec-bee3-08a1a3b957f8 | c01_Main_GetBackDoorKey | Boolean | False |  |  |  |

### c01_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 889fd052-4339-4fb4-920c-a6ac99eb6fd7 | PL_ChainCutterGet | Boolean | False |  |  |  |
| Unknown | 97b25191-1197-4e97-9022-39abfedc20f4 | PL_FuseGet | Boolean | False |  |  |  |
| Unknown | 410e3602-ecfe-4302-a691-22ea61dc3cc4 | PL_ArmCut | Boolean | False |  |  |  |
| Unknown | 79f62641-4da0-4049-b236-d945e05d1da0 | PL_ArmCare | Boolean | False |  |  |  |
| Likely | 74d35253-f19a-4f9e-ae40-8566a7686813 | PL_CutHandChange | Boolean | False | Guest house prologue flow: controls Ethan's missing-hand model/state after Mia cuts off his hand. | Read by `1100_PLHandON` in `levelfsm_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=PL_CutHandChange (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`) |
| Likely | a3b90e87-2ac3-4076-96d5-e89c00c3cac8 | PL_FF00GetAway | Boolean | False | Guest house prologue flow: marks that the Sewer Gators tape escape/ending state has been recorded for main-story callbacks. | Written by `500_ChapterJump` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets PL_FF00GetAway true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=PL_FF00GetAway (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`) |
| Likely | 5232b4fb-5ee2-4edd-a2cc-a9c3d423ec10 | Mia_WalkCancelable | Boolean | False | Guest house prologue flow: controls whether Mia's guided walk can be cancelled/interrupted. | Read by `300_WalkToNarrow` in `mia_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Mia_WalkCancelable (`leveldesign/fsm/chapter1/mia_c01.scn.20`) |
| Likely | 7b4f178b-d6df-4d69-803c-9e2ed23dca64 | Mia_WalkStart | Boolean | False | Guest house prologue flow: tracks the start of Mia's guided walk sequence. | Read by `300_WalkToNarrow` in `mia_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Mia_WalkStart (`leveldesign/fsm/chapter1/mia_c01.scn.20`) |
| Likely | 04f50c49-e50d-4c8c-93b8-e828f0330bf7 | Mia_WakeUpEnd | Boolean | False | Guest house prologue flow: tracks whether Mia's wake-up sequence has ended. | Read by `300_WalkToNarrow` in `mia_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Mia_WakeUpEnd (`leveldesign/fsm/chapter1/mia_c01.scn.20`) |
| Likely | 996887f6-57c9-40c9-8da2-414772560637 | Mia_JailOut | Boolean | False | Guest house prologue flow: tracks that Mia has left the jail/cell area. | Read by `300_JailCollisionPL_Off` in `mia_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Mia_JailOut (`leveldesign/fsm/chapter1/mia_c01.scn.20`) |
| Likely | bbba9f22-822b-402d-a6b0-9ee4fbb5082c | NarrowAreaOn | Boolean | False | Guest house prologue flow: toggles the narrow-area collision/gimmick state in the guest house. | Read by `300_NarrowArea_OFF` in `levelfsm_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=NarrowAreaOn (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`) |
| Likely | 434d77f4-69db-44b7-9c42-f9da3fc44da9 | Rating_PL_ArmCutBlackOut | Boolean | False | Guest house prologue flow: tracks the blackout/rating event around Ethan's hand-cut sequence. | Read by `1100_PLHandON` in `levelfsm_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Rating_PL_ArmCutBlackOut (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`) |
| Likely | 79dc80a3-95c5-43c7-9d8b-bdd64cb59961 | Mia_WalkEnd | Boolean | False | Guest house prologue flow: tracks the end of Mia's guided walk sequence. | Read by `300_MiaEventFsmTriggerActive` in `mia_c01.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Mia_WalkEnd (`leveldesign/fsm/chapter1/mia_c01.scn.20`) |

### c03_1_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Confirmed | 6c048575-16b6-4de6-b922-15570d588e9f | c03_1_Main_PLFree | Boolean | False | Player regains control after the dinner opening. | Set by `0_DinnerEventEnd`. | `levelfsm_c03_1.scn.20`: `WaitNPCEventEnd_SetBool` writes this GUID true. |
| Likely | 69c18efc-12bd-4f8d-b03d-0037c357df55 | c03_1_Main_LDKLightChange | Boolean | False | Main house opening/garage flow: marks the living/dining/kitchen lighting change after the dinner escape. | Written by `100_Main_LDKLightChange` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_Main_LDKLightChange true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Gate package `c03_1_main_200.user.2` |
| Likely | 024d7582-3a98-4587-9b4f-a4dc47cd2cb4 | c03_1_Main_GetFloorDoorKey | Boolean | False | Main house opening/garage flow: marks obtaining the floor hatch key. | Written by `200_Main_GetFloorDoorKey` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_Main_GetFloorDoorKey true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Gate package `c03_1_main_300.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_Main_GetFloorDoorKey (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | f2095062-1d8f-46f1-a9b3-b1a75b75842c | c03_1_Main_GoDownUnderFloor | Boolean | False | Main house opening/garage flow: marks going down through the floor hatch into the crawlspace. | Written by `300_Main_GoDownUnderFloor` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_Main_GoDownUnderFloor true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Gate package `c03_1_main_400.user.2` |
| Likely | 7b6d9613-fcf0-4582-91db-a59ba59895ed | c03_1_Main_EnterWashRoom | Boolean | False | Main house opening/garage flow: marks entry into the laundry/wash-room safe area. | Written by `400_EnterWashRoom` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_Main_EnterWashRoom true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Gate package `c03_1_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_Main_EnterWashRoom (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | a3d79f74-d813-4742-8cf5-2c0f6bdc1932 | c03_1_Main_InteractHallDoorAndHitTrigger | Boolean | False | Main house opening/garage flow: marks trying the hall door/triggering the forced phone progression. | Written by `500_Main_ForceAdvance_PhoneMesGetAfter60second` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_Main_InteractHallDoorAndHitTrigger true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Gate package `c03_1_main_600.user.2` |
| Unknown | 8daa397f-0332-4c8a-8217-5abb6c271cfc | c03_1_Main_PoliceKnockWindow | Boolean | False |  |  |  |
| Confirmed | 6b257b54-0c7c-489e-b5f8-75b84df5a318 | c03_1_Main_PolicegiveKnife | Boolean | False | Deputy gives the knife. | Set by `600_Main_DaveMeetEventEnd`. | `levelfsm_c03_1.scn.20`: Dave meet event FSM writes this GUID true. |
| Confirmed | ebeadb20-54cd-46c1-a47c-a6972dd6ecfa | c03_1_Main_BreakerOn | Boolean | False | Breaker box is opened/on. | Set by `700_CK_OpenBox`. | `levelfsm_c03_1.scn.20`: `FsmStateCheck` waits for the box `OpenState`, then `SetBool`. |
| Likely | b45d3586-e265-4610-a42c-969b14c2ff9f | c03_1_Main_SwitchOnGarageShutter | Boolean | False | Main house opening/garage flow: marks switching on the garage shutter before the Jack garage fight. | Written by `800_SwitchOnGarageShutter` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_Main_SwitchOnGarageShutter true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Gate package `c03_1_main_900.user.2` |
| Confirmed | 8d23c6ea-b3f4-4c7c-ac1c-8bc75a8dc3da | c03_1_Main_MainHallGoInto | Boolean | False | Player has entered the main hall. | Set by `1100_main_MainHallGoInto`. | `levelfsm_c03_1.scn.20`: trigger `SetBool` writes true. |
| Confirmed | 22901bdb-1b8d-425f-92c8-73ef6d00f861 | c03_1_Main_GarageBattleEnd | Boolean | False | Garage Jack battle has ended. | Set by `900_JackGarageBattleEnd`. | `levelfsm_c03_1.scn.20`: `app.WaitEmBattleEnd` followed by `SetBool`. |
| Confirmed | 4ea83ccd-8733-4d66-8293-3943075c21a3 | c03_1_Main_JackCrushingEventEnd | Boolean | False | Jack crushing/headshot event has ended after the garage fight. | Set by `1000_JackHeadShotWait`. | `levelfsm_c03_1.scn.20`: event FSM waits for `c03e03_01` and `WaitNPCEventEnd`, then writes true. |

### c03_1_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 797eee21-0fa5-4266-ad5b-a26d3145e843 | c03_1_AnswerTelephone | Boolean | False |  |  |  |
| Unknown | 408bff4e-60a6-41cb-9bdf-b3036c51df9e | c03_1_DaveDefectHead | Boolean | False |  |  |  |
| Unknown | f27cd1c8-e676-4d19-bbf4-2657793327b8 | c03_1_JackDefectHead | Boolean | False |  |  |  |
| Likely | e5f93392-5b46-4a89-86ed-a6f53e53d028 | c03_1_GazeAtPictureInTH | Boolean | False | Main house opening/garage flow: tracks looking at the trailer-house picture/clue used by the hall-door progression. | Read by `500_InteractHallDoorAndHitTrigger` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_InteractHallDoor (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 8982620b-d285-4658-8872-93b90ef76d92 | c03_1_JackBreakDownTable | Boolean | False | Main house opening/garage flow: tracks Jack breaking the dining-room table during the dinner escape. | Read by `200_EffectOn_DishEffect_TableBreak` in `common_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackBreakDownTable (`leveldesign/fsm/chapter3/common_c03_1.scn.20`) |
| Likely | 6d8868d8-d18f-45d9-8caa-f02fde6a61ec | c03_1_JackCutLegStart | Boolean | False | Main house opening/garage flow: marks the start of Jack's leg-cut attack sequence. | Read by `300_DoorClose_JackCutLegStart` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackCutLegStart (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | a4814478-507a-4c34-a1f1-925cba3b90cc | c03_1_JackCutLeg | Boolean | False | Main house opening/garage flow: tracks that Jack's leg-cut tutorial/event state is active. | Read by `Tutorial_CutLeg` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackCutLeg (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 883b5bf5-c3cb-4d7d-9984-4c4daa5174c8 | c03_1_JackCutLegEnd | Boolean | False |  |  |  |
| Unknown | 3b4dd88e-54ec-4eaf-a09c-b158c1b9292b | c03_1_DinnerEscapePl0000Seq | Int32 | 0 |  |  |  |
| Unknown | f0bbab28-ae83-4825-b35d-12334622062a | c03_1_GaregeCarPosithionL01 | Boolean | False |  |  |  |
| Unknown | 58c0058c-e33a-42a3-90be-d86bfbf9be46 | c03_1_GaregeCarPosithionL02 | Boolean | False |  |  |  |
| Unknown | 86cf5d54-3e23-4e52-9dd9-74125b6c602f | c03_1_GaregeCarPosithionR01 | Boolean | False |  |  |  |
| Unknown | e5dee5ac-93f2-49b1-9d0f-44b7eea69d37 | c03_1_GaregeCarPosithionR02 | Boolean | False |  |  |  |
| Unknown | a0369110-78af-4f9e-9a06-fd936d8d6b0c | c03_1_JackDefectHeadOnly | Boolean | False |  |  |  |
| Likely | e86abf8c-e0f7-4593-b49e-84a38d3c5381 | c03_1_JackBreakWall | Boolean | False | Main house opening/garage flow: tracks Jack breaking through the wall during the main-house pursuit. | Written by `200_JackWallBreak` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_JackBreakWall false (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackBreakWall (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Unknown | d2180b82-4fd8-4e94-b369-59cc9f5b0a40 | c03_1_LucasHoodOn | Boolean | False |  |  |  |
| Unknown | 272d2f98-3f14-4503-b0d5-be0ba89d2c1a | c03_1_LucasHoodOnAndArmOff | Boolean | False |  |  |  |
| Unknown | 847b24be-ea6d-42a7-92bc-85d05d268873 | c03_1_EntranceDoorNotOpenMessage | Boolean | False |  |  |  |
| Likely | e49d1453-2b71-49b9-8808-f0d2bbce0958 | c03_1_JackDissapearInGarage | Boolean | False | Main house opening/garage flow: tracks Jack disappearing/repositioning during the garage fight. | Read by `900_BGM_BattleInGarage` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackDissapearInGarage (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 1c6b98b0-8d23-4ec0-88be-631f81264875 | c03_1_JackStartCrash | Boolean | False | Main house opening/garage flow: marks the start of Jack's garage crash sequence. | Read by `900_BGM_BattleInGarage` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackStartCrash (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 1d4ffe58-967b-41c2-8b3f-5bc9b1ba2a34 | c03_1_SmokeInGarage | Boolean | False | Main house opening/garage flow: enables the garage smoke effect after the crash. | Read by `900_SmokeInGarage` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_SmokeInGarage (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 37d3e997-a3da-45f0-835b-5e6acdc0ae88 | c03_1_JackGetPlayerOut | Boolean | False | Main house opening/garage flow: tracks Jack pulling/forcing the player out during the garage fight. | Read by `900_Fsm_GarageBattle` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackGetPlayerOut (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 090f58ee-c273-44f5-b735-1331c510cead | c03_1_PlayerStartEngine | Boolean | False | Main house opening/garage flow: tracks the player starting the car engine in the garage fight. | Read by `900_em3000_GetIntoCar` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_PlayerStartEngine (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | dd5d639e-326c-461d-bbb2-d9299dcee034 | c03_1_BreakShelfInGarage | Boolean | False | Main house opening/garage flow: tracks shelf destruction during the garage fight. | Written by `900_Fsm_GarageBattle` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_BreakShelfInGarage true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_BreakShelfInGarage (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | f512bdca-bdaf-4703-b2c9-ce6ded1a8c85 | c03_1_PlayerGetIntoCancel | Boolean | False | Main house opening/garage flow: tracks/cancels the player's attempt to get into the car. | Written by `900_Fsm_GarageBattle` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_PlayerGetIntoCancel false (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_PlayerGetIntoCancel (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Unknown | 3244d35d-8b4b-4364-b70c-71014301467b | c03_1_GearDrive | Boolean | False |  |  |  |
| Unknown | d2b26190-ac09-4cce-bf71-64f179a05365 | c03_1_ChangeGear | Boolean | False |  |  |  |
| Unknown | 328ff0d1-0952-423c-84df-8657bb1d2435 | c03_1_ChangeGearDrive | Boolean | False |  |  |  |
| Unknown | 5a80d005-287d-46ae-b226-262727086915 | c03_1_ChangeGearBack | Boolean | False |  |  |  |
| Likely | f57e3b82-8afe-4adf-81b1-b143e811002e | c03_1_StartAccidentInGarage | Boolean | False | Main house opening/garage flow: marks the start of the garage car-accident sequence. | Read by `900_Fsm_GarageBattle` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_StartAccidentInGarage (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 6724feb8-090a-4779-a7d9-c998d7d98ca6 | c03_1_TutorialDriving | Boolean | False | Main house opening/garage flow: controls the garage driving tutorial prompt/state. | Written by `900_TutorialDriving` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_TutorialDriving false (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_TutorialDriving (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 74fe4b43-1c1d-4421-a56e-b5bde32bfb67 | c03_1_JackDiscoverFirst | Boolean | False | Main house opening/garage flow: tracks Jack's first discovery/search state in the main house. | Read by `200_JackWallBreak` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=c03_1_JackDiscoverFirst (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 92e052ab-c9fb-4694-9de1-e425a5d06dca | c03_1_JackGetIntoCarDueToTimeLimit | Boolean | False | Main house opening/garage flow: marks Jack getting into the car after the garage time limit. | Written by `900_em3000_GetIntoCar` in `levelfsm_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_JackGetIntoCarDueToTimeLimit true (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Unknown | 367319e3-66ad-4240-a0eb-a6cb5ccf450a | c03_1_TutorialDrivingEnd | Boolean | False |  |  |  |
| Likely | 4464ddfc-6cb8-41e4-8a74-25440a8fcd05 | c03_1_JackRestMessageEnd | Boolean | False | Main house opening/garage flow: tracks the end of Jack's rest/idle message branch. | Read by `400_ActiveOff_Jack_InUnderThefloor` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackRestMessageEnd (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Unknown | c854b949-3cdc-4fb5-826d-16bdf63add44 | c03_1_JackWearingThinBeforeDrive | Boolean | False |  |  |  |
| Unknown | 145ac7ac-8ee2-448b-b0d2-3b1f39c8193c | c03_1_AccidentHitWallInGarage | Boolean | False |  |  |  |
| Likely | 4c960640-c521-48e2-9d71-514507a650f4 | c03_1_AccidentHitSteelFrameInGarage | Boolean | False | Main house opening/garage flow: tracks the garage car crash into the steel frame. | Read by `900_BreakShelfOnOff` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_AccidentHitSteelFrameInGarage (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Unknown | 5c96fa6a-1c44-4c49-9232-5cf4f1e95c53 | c03_1_JackRideCar | Boolean | False |  |  |  |
| Likely | 7a79f0e2-7602-4205-a49a-729f576983b5 | c03_1_PLGettingUp_AfterCarExploded | Boolean | False | Main house opening/garage flow: tracks Ethan getting up after the car explodes. | Read by `1000_ActiveOn_DownLadder_ActiveOff_UpperLadder` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_PLGettingUp_AfterCarExploded (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Likely | 94564659-805d-499c-b0dd-8527ccea0fa4 | c03_1_JackGetOutOfCar | Boolean | False | Main house opening/garage flow: tracks Jack exiting the car during the garage fight. | Read by `900_BreakShelfOnOff` in `levelfsm_c03_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_1_JackGetOutOfCar (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |
| Unknown | cf8ae833-3ae8-4db3-ba4f-8cde4bfe0183 | c03_1_StartJackSuicide | Boolean | False |  |  |  |
| Likely | cbe9528a-9d70-4c8f-a049-ac39e42b7b1a | c03_1_TaxdermyAreaPhoto_Interacted | Boolean | False | Main house opening/garage flow: tracks interaction with the taxidermy-area treasure photo. | Written by `0_OtakaraMapPopUpItem02_TaxidermyB1F_FFS` in `common_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_TaxdermyAreaPhoto_Interacted true (`leveldesign/fsm/chapter3/common_c03_1.scn.20`) |
| Likely | fad593c7-2ae0-4c04-9f56-5f96a2d7b458 | c03_1_MainHall2FEastAreaPhoto_Interacted | Boolean | False | Main house opening/garage flow: tracks interaction with the main-hall 2F treasure photo. | Written by `0_OtakaraMapPopUpItem03_MainHall2FEast_FFS` in `common_c03_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_1_MainHall2FEastAreaPhoto_Interacted true (`leveldesign/fsm/chapter3/common_c03_1.scn.20`) |

### Weather_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 02f81596-a182-4700-ae56-5c7d04ea0d70 | Rain | Boolean | False | Weather state: enables the rainy exterior weather setup. | Written by `200_FlagSet_Rain` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets Rain true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`) |

### FF000_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 3c2d3411-ed08-49d2-9fd4-76a489ff83f7 | FF000_Main_c01f01_00Finish | Boolean | False | Sewer Gators videotape flow: marks the first Sewer Gators intro cut/segment as finished. | Written by `0_c01f01_00Finish` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_Main_c01f01_00Finish true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Gate package `ff000_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF000_Main_c01f01_00Finish (`leveldesign/fsm/ff000/objectivefsm_ff000.scn.20`) |
| Likely | ef2c34d3-c32d-43f4-b123-4e3bb18d32d0 | FF000_Main_IntaractBackDoor | Boolean | False | Sewer Gators videotape flow: marks interacting with the guest-house back door during the Sewer Gators tape. | Written by `100_ApproachingBackDoor` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_Main_IntaractBackDoor true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Gate package `ff000_main_200.user.2` |
| Likely | 7e979f73-4a96-4d4f-ad32-1a59e456e60c | FF000_Main_c01f01_50Finish | Boolean | False | Sewer Gators videotape flow: marks the next Sewer Gators cut/segment as finished. | Required by game-flow package `ff000_main_300.user.2`. | Gate package `ff000_main_300.user.2` |
| Likely | 2198dfd9-ebf2-410f-bcc2-10f09b0b1df1 | FF000_Main_PeteStoveSwitchOn | Boolean | False | Sewer Gators videotape flow: marks Pete turning on/interacting with the kitchen stove switch. | Written by `300_PeteStoveSwitchOn` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_Main_PeteStoveSwitchOn true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Gate package `ff000_main_400.user.2` |
| Likely | 7cd3f90e-b7ad-4ac0-9c46-1c74eb4aa27a | FF000_Main_c01f04_00Finish | Boolean | False | Sewer Gators videotape flow: marks the Sewer Gators secret-passage segment as finished. | Written by `400_c01f04_00Finish` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_Main_c01f04_00Finish true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Gate package `ff000_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF000_Main_c01f04_00Finish (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`) |
| Likely | a21cff37-f62c-448c-8ece-4912abdd2ee4 | FF000_Main_ChapterJump | Boolean | False | Sewer Gators videotape flow: marks the Sewer Gators tape exit/return to the main story. | Written by `500_ChapterJump` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_Main_ChapterJump true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Gate package `ff000_main_end.user.2` |

### FF000_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 4096b554-e37a-4c55-9446-68d4f7f03b41 | Pete_StoveSwitchOn | Boolean | False | Sewer Gators videotape flow: toggles pete stove switch on. | Read by `300_PeteStoveSwitchOn` in `levelfsm_ff000.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Pete_StoveSwitchOn (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`) |
| Likely | c952a2c9-b54e-41c0-9a9d-9d2d66d52571 | Pl_IntoKitchen | Boolean | False | Sewer Gators videotape flow: marks that the player entered player into kitchen. | Written by `300_TriggerInAction_SetBool_Pl_IntoKitchen` in `npc_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets Pl_IntoKitchen true (`leveldesign/fsm/ff000/npc/npc_ff000.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=NpcPositionRecordMode (`leveldesign/fsm/ff000/npc/npc_ff000.scn.20`) |
| Likely | 4152e059-e138-4e34-8b67-e35ca9c0a227 | SecretDoorOpen | Boolean | False | Sewer Gators videotape flow: marks that secret door open is open. | Read by `400_sm0287_LivingSecretDoorSlowOpen` in `levelfsm_ff000.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=SecretDoorOpen (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`) |
| Likely | 385bae74-3683-466e-b0cf-a9d0f2cb33aa | Npc_WalkCancelable | Boolean | False | Sewer Gators videotape flow: tracks npc walk cancelable. | Read by `300_KitchenAndLiving` in `npc_ff000.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Npc_WalkCancelable (`leveldesign/fsm/ff000/npc/npc_ff000.scn.20`) |
| Likely | 2815c64b-a9eb-4577-85d9-ec1c53d84a09 | Npc_BackDoorArrived | Boolean | False | Sewer Gators videotape flow: tracks npc back door arrived. | Read by `100_Goto_StartDoor` in `npc_ff000.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Npc_BackDoorArrived (`leveldesign/fsm/ff000/npc/npc_ff000.scn.20`) |
| Likely | c426c19a-a628-456a-9ae8-12000c9d40d9 | Pl_GoOutKitchen | Boolean | False | Sewer Gators videotape flow: tracks player go out kitchen. | Written by `300_TriggerInAction_SetBool_Pl_GoOutKitchen` in `npc_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets Pl_GoOutKitchen true (`leveldesign/fsm/ff000/npc/npc_ff000.scn.20`) |
| Unknown | bab7e392-f77b-4b5f-9d1b-2bfe67af7e1f | Pete_MessageFinished_ByLadder | Boolean | False |  |  |  |
| Likely | 985252cb-b812-4d9d-901d-0dc5529fea1f | FF000_KeyPickDrawerOpen | Boolean | False | Sewer Gators videotape flow: tracks videotape 000 key pick drawer open. | Written by `0_KeyPickDrawerOpen_SetBool` in `levelfsm_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_KeyPickDrawerOpen true (`leveldesign/fsm/ff000/levelfsm_ff000.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF000_KeyPickDrawerOpen (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`) |

### c03_3_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Confirmed | f306f3cc-3d72-4fae-b431-14ab53f037c9 | c03_3_Main_OpenMainHallDoor | Boolean | False | Main hall door has been opened for old house progression. | Set by `0_Main_OpenMainHallDoor`. | `levelfsm_c03_3.scn.20`: trigger `SetBool` writes true. |
| Confirmed | 16b1ebed-9b42-4339-bba7-692323b431b1 | c03_3_Main_EnterTrailerHouse | Boolean | False | Player has entered the trailer house. | Set by `100_Main_EnterTrailerHouse`. | `levelfsm_c03_3.scn.20`: trigger `SetBool` writes true. |
| Likely | c06ab2c4-f95d-46e0-83dc-c0c749102e34 | c03_3_Main_HangUpPhoneFromZoe | Boolean | False | Old house/Marguerite flow: marks hanging up Zoe's phone call before entering the Old House. | Written by `125_Main_HangUpPhoneFromZoe` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_HangUpPhoneFromZoe true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_150.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_3_Main_HangUpPhoneFromZoe (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 17e5af29-0cab-4e3c-a78d-71ee87798b6c | c03_3_Main_EnterMiaCapturedRoom | Boolean | False | Old house/Marguerite flow: marks entering the room where Mia was captured in the Old House. | Written by `150_Main_EnterMiaCapturedRoom` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_EnterMiaCapturedRoom true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_200.user.2` |
| Likely | 779d13a7-5296-4a4a-bf04-c92e8930cc90 | c03_3_Main_PassThroughFirePlaceAgain | Boolean | False | Old house/Marguerite flow: marks using the fireplace passage again after the Old House intro. | Written by `200_Main_PassThroughFirePlaceAgain` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_PassThroughFirePlaceAgain true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_300.user.2` |
| Likely | e4ef4f89-4d98-4d81-86a0-8ea640eac4dc | c03_3_Main_GetCrank | Boolean | False | Old house/Marguerite flow: marks obtaining the crank in the Old House. | Written by `300_Main_GetCrank` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_GetCrank true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_400.user.2` |
| Likely | 6ed99e11-2047-4236-84a0-6457c7a3b1c9 | c03_3_Main_TalismanKeyGet | Boolean | False | Old house/Marguerite flow: marks obtaining the crow key. | Written by `400_Main_TalismanKeyGet` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_TalismanKeyGet true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_500.user.2` |
| Likely | c0b76ccc-ceb2-4af0-acd0-0b666210f380 | c03_3_Main_InsectFallEventLineOn | Boolean | False | Old house/Marguerite flow: enables the event line for the insect-fall encounter. | Written by `500_Main_InsectFallEventLineOn` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_InsectFallEventLineOn true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_600.user.2` |
| Confirmed | 965eeafb-d0f3-4f97-8b3a-00b74c941264 | c03_3_Main_InsectFallBattleFinish | Boolean | False | Old house insect fall battle has finished. | Set by `600_Main_InsectFallBattleFinish`. | `levelfsm_c03_3.scn.20`: `app.WaitEmBattleEnd` followed by `SetBool`. |
| Likely | d68dbec2-220f-4f36-af89-2eb62de71969 | c03_3_Main_PhonePickUp_InteractLanternDoor | Boolean | False | Old house/Marguerite flow: marks the phone pickup after interacting with Marguerite's lantern door. | Written by `700_Main_PhonePickUp_InteractLanternDoor` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_PhonePickUp_InteractLanternDoor true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_800.user.2` |
| Likely | 25fb24a6-8a8c-48d6-a368-e05002c5b322 | c03_3_Main_ApproachingInsectHole | Boolean | False | Old house/Marguerite flow: marks approaching the insect hole before the Marguerite encounter route. | Written by `200_Gobogobo2` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_InsectFallEventFinish true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Gate package `c03_3_main_900.user.2` |
| Likely | 5eded453-246f-4ce6-880c-2a4bbe8c865b | c03_3_Main_GoUpToWell | Boolean | False | Old house/Marguerite flow: marks climbing up from the well/tunnel route. | Written by `900_Main_GoUpToWell` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_GoUpToWell true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_950.user.2` |
| Likely | 09ef768c-01b3-4141-b9dc-6a4b2d257347 | c03_3_Main_FallHoleBeforeLongHandMargueriteBattle | Boolean | False | Old house/Marguerite flow: marks falling through the hole before the long-arm Marguerite battle. | Written by `950_Main_FallHoleBeforeLongHandMargueriteBattle` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_FallHoleBeforeLongHandMargueriteBattle true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_1000.user.2` |
| Confirmed | 46563534-a650-40ef-be43-155a02278302 | c03_3_Main_LongHandMargueriteBattleFinish | Boolean | False | Long-arm Marguerite battle has finished. | Set by `1000_Main_LongHandMargueriteBattleFinish`. | `levelfsm_c03_3.scn.20`: `app.WaitEmBattleEnd` followed by `SetBool`. |
| Confirmed | acb7e0bd-e123-4a57-8a0b-1bf77087e856 | c03_3_Main_GetLantern | Boolean | False | Lantern has been obtained or inspected after Marguerite. | Set by `1100_Main_GetLanternOrWatchLantern`. | `levelfsm_c03_3.scn.20`: `ItemTest` checks `Lantern`, then `SetBool`. |
| Likely | 9b1dd2cd-3fea-41da-98cd-ba35bb079873 | c03_3_Main_SolveLanternGimmick | Boolean | False | Old house/Marguerite flow: marks solving the lantern-weight door puzzle. | Written by `1200_Main_SolveLanternGimmick` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_Main_SolveLanternGimmick true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`); Gate package `c03_3_main_1300.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_3_Main_SolveLanternGimmick (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Confirmed | e4b4b42e-ecfc-415e-a713-e1a3604af371 | c03_3_Main_GetEvlineArm | Boolean | False | D-series arm has been acquired. | Set by `1300_Main_GetEvlineArm`. | `levelfsm_c03_3.scn.20`: `ItemTest` checks `SerumMaterialA`, then `SetBool`. |
| Likely | f4bf6a88-ccd2-4614-87aa-59d77cae3754 | c03_2_Main_GetSnakeKey | Boolean | False | Old house/Marguerite flow: marks obtaining the snake key after returning from the Old House. | Written by `1400_Main_GetSnakeKey` in `levelfsm_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_2_Main_GetSnakeKey true (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`) |
| Unknown | 05fa51f6-095c-4b6c-a21b-505167a7f1fd | c03_3_Main_HangUpLucasPhone | Boolean | False |  |  |  |

### c03_3_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 244c8eef-1a3d-4c8e-b135-d93e6a9b2e77 | ShadowPuzzle_InsectKill | Boolean | False | Old house/Marguerite flow: tracks killing/clearing the insect at the Old House shadow puzzle. | Read by `260_ShadowPuzzle` in `levelfsm_ff030.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=ShadowPuzzle_InsectKill (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`) |
| Unknown | 99032114-fa1f-4928-91a2-f72827bc1003 | c03_3_Chapter3-4Start | Boolean | False |  |  |  |
| Unknown | c0109d34-7735-4f79-9dc8-3abf668c77d1 | PL_YakugakuScan | Boolean | False |  |  |  |
| Unknown | 4515a6af-0a3c-4b8f-8fac-cd8016b77fd1 | c03_3_ComeBackFromOldHouse | Boolean | False |  |  |  |
| Likely | 6406efeb-3474-4c3b-b3cc-00f0825457df | c03_3_LongHandMotherAppearance | Boolean | False | Old house/Marguerite flow: tracks long-arm Marguerite appearing for the greenhouse boss fight. | Read by `1000_InGameContentAction_FinalMotherBattleStart` in `levelfsm_c03_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_3_LongHandMotherAppearance (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`) |
| Likely | 8ae695ce-d68e-4b29-abdb-75ac90650e75 | c03_3_AnswerPhone_OldHouse2F1st | Boolean | False | Old house/Marguerite flow: tracks the first Old House 2F phone call. | Written by `PhoneMessage_200_OldHouse2F00` in `phonemessage_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_AnswerPhone_OldHouse2F1st true (`leveldesign/fsm/chapter3/chapter3_3/phonemessage_c03_3.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_3_AnswerPhone_OldHouse2F1st (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`) |
| Likely | 61f87dac-8df2-4eee-93ca-971f3ef614ca | c03_3_AnswerPhone_OldHouse2F2nd | Boolean | False | Old house/Marguerite flow: tracks the second Old House 2F phone call. | Written by `PhoneMessage_300_OldHouse2F01` in `phonemessage_c03_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_3_AnswerPhone_OldHouse2F2nd true (`leveldesign/fsm/chapter3/chapter3_3/phonemessage_c03_3.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=c03_3_AnswerPhone_OldHouse2F2nd (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`) |
| Unknown | 66b770f2-90ee-491e-8663-65e61fe44d98 | c03e31_02_em2000_RandomTalk | Int32 | 0 |  |  |  |
| Likely | 8832e318-325f-4b16-a699-db4f86e6e2aa | c03_3_LongHandMotherBeginToCollapse | Boolean | False | Old house/Marguerite flow: tracks Marguerite beginning to collapse after the boss fight. | Read by `1000_MSG_AfterLongHandMotherBattle_FFS` in `levelfsm_c03_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_3_LongHandMotherBeginToCollapse (`leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20`) |
| Likely | d39b062f-5ce7-4955-adf5-6b215da778be | c03_3_MargueriteWanderingEnd | Boolean | False | Old house/Marguerite flow: tracks the end of Marguerite's wandering/search behavior. | Read by `300_AfterSottojiEvent_SetMargueriteThinkState00` in `marguerite_c03_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_3_MargueriteWanderingEnd (`leveldesign/fsm/chapter3/chapter3_3/marguerite_c03_3.scn.20`) |

### FF030_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 87968125-5dba-4d6a-96a5-c0b67ab65009 | FF030_Main_MiaMonologueFinish | Boolean | False | Mia videotape flow: marks Mia's opening monologue in the Old House videotape as finished. | Written by `0_OpeningEventSet` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_MiaMonologueFinish true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF030_Main_MiaMonologueFinish (`leveldesign/fsm/ff030/objectivefsm_ff030.scn.20`) |
| Likely | 342db8a6-95d1-4b98-9ff3-2b730bbc9e65 | FF030_Main_FirstMargueriteDisappear | Boolean | False | Mia videotape flow: marks Marguerite's first disappearance in the Mia videotape. | Written by `100_FirstMargaretDisappeared` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_FirstMargueriteDisappear true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_200.user.2` |
| Likely | f27e9658-4e95-4d4b-90f2-42c3230d48f3 | FF030_Main_ApproachingShadowPuzzle | Boolean | False | Mia videotape flow: marks approaching the shadow puzzle in the Mia videotape. | Written by `200_ApproachingShadowPuzzle` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_ApproachingShadowPuzzle true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_230.user.2` |
| Likely | a63dfb43-f9e5-4c44-8e85-bde5b8e49055 | FF030_Main_MargueriteWalkToPier | Boolean | False | Mia videotape flow: marks Marguerite walking toward the pier in the Mia videotape. | Written by `230_Main_MargueriteWalkToPier` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_MargueriteWalkToPier true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_245.user.2` |
| Likely | c91a711e-426c-40e9-bf3d-c19644eaec6f | FF030_Main_MargueriteGoOutFromPuzzleRoom | Boolean | False | Mia videotape flow: marks Marguerite leaving the puzzle room in the Mia videotape. | Written by `245_MargueriteGoOutFromPuzzleRoom` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_MargaretGoOutFromPuzzleRoom true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_260.user.2` |
| Likely | 2d113c82-aeab-452a-a454-d8e38e0ed8f4 | FF030_Main_SolvePazzle | Boolean | False | Mia videotape flow: marks solving the shadow puzzle in the Mia videotape. | Written by `260_ShadowPazzleFinish` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_SolvePazzle true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_300.user.2` |
| Likely | ea150ffb-9fb2-45c5-a0a3-7e6ecb58080b | FF030_Main_InteractEvlinRadar | Boolean | False | Mia videotape flow: marks interacting with the Eveline tracker/photo clue in the Mia videotape. | Written by `300_Interact_MiaEvlinPicture01A` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_GetCrank true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_400.user.2` |
| Likely | f40ed8dc-8054-4680-b3a1-3fc528d083cc | FF030_Main_GrapEventMarguerite | Boolean | False | Mia videotape flow: marks Marguerite's grab event in the Mia videotape. | Written by `400_AfterGrapEventMarguerite` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_GrapEventMarguerite true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF030_Main_GrapEventMarguerite (`leveldesign/fsm/chapter3/common_c03_2.scn.20`) |
| Likely | 2a0faa90-ea24-401e-b4d3-667f40d1a56e | FF030_Main_ChapterJump | Boolean | False | Mia videotape flow: marks the Mia videotape exit/return to the main story. | Written by `500_AfterChapterJump` in `levelfsm_ff030.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF030_Main_ChapterJump true (`leveldesign/fsm/ff030/levelfsm_ff030.scn.20`); Gate package `ff030_main_end.user.2` |

### FF030_Flag

_No variables were listed under this group in the captured baseline log._

### c03_2_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Confirmed | aac5e34f-fb19-4416-8cd0-6908953f519f | c03_2_Main_EnterHallArea | Boolean | False | Player has entered the main hall area. | Set by `0_EnterHallArea`. | `levelfsm_c03_2.scn.20`: trigger `SetBool` writes true. |
| Confirmed | e165ff7a-0829-4edc-8c34-68a01a1ff3b2 | c03_2_Main_GetPazzleObject | Boolean | False | Shadow puzzle object acquired/interacted. | Set by `100_GetPazzleObject`. | `levelfsm_c03_2.scn.20`: `InteractTest_SetBool` writes true after puzzle object interaction. |
| Confirmed | 60153105-ece3-4952-8a31-ef7af7665fbe | c03_2_Main_SolvePazzle | Boolean | False | Main hall shadow puzzle solved. | Set by `200_SolvePuzzle`. | `levelfsm_c03_2.scn.20`: `FsmStateCheck` waits for `StateCheck`, then `SetBool`. |
| Likely | b3096800-d600-4015-b934-63d671b597a9 | c03_2_Main_OpenTrayInWorkshopKey | Boolean | False | Main house and processing-area flow: marks opening the workshop tray that holds the dissection-room key. | Written by `400_OpenTrayInWorkshopKey` in `levelfsm_c03_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_2_Main_OpenTrayInWorkshopKey true (`leveldesign/fsm/chapter3/chapter3_2/levelfsm_c03_2.scn.20`); Gate package `c03_2_main_500.user.2` |
| Likely | ed2860cf-2569-4045-96c8-ba01e0fcfed8 | c03_2_Main_GetCrestInFreezerRoom | Boolean | False | Main house and processing-area flow: marks obtaining the final dog head/crest in the dissection-room/freezer route. | Written by `500_GetCrestInFreezerRoom` in `levelfsm_c03_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_2_Main_GetCrestInFreezerRoom true (`leveldesign/fsm/chapter3/chapter3_2/levelfsm_c03_2.scn.20`); Gate package `c03_2_main_600.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_2_Main_SatisfyJackBattleStartConditions (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Confirmed | 405f2e67-3749-4410-9dd6-776ce7bcd583 | c03_2_Main_JackBattleEnd | Boolean | False | Jack 2 chainsaw battle has ended. | Set by `600_JackBattleEnd`. | `levelfsm_c03_2.scn.20`: `app.WaitEmBattleEnd` followed by `SetBool`. |
| Confirmed | b617572c-527c-45df-b508-737225f5310c | c03_2_Main_FitInAllCrestEnd | Boolean | False | All dog heads/crests fitted and the 3C door flow is open. | Set by `700_Open_3CDoor`. | `levelfsm_c03_2.scn.20`: `FsmStateCheck` waits for `StateCheck_Open`, then `SetBool`. |

### c03_2_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 4eacbfba-5846-4995-b1b7-1bd8eb290449 | c032_CanUseMedicineRecipe | Boolean | False |  |  |  |
| Likely | 001907a3-b52c-437b-b656-a8717dcbdfd9 | c03_2_AnswerPhone_MainHall | Boolean | False | Main house and processing-area flow: tracks Zoe's main-hall phone call after reaching the main house. | Written by `PhoneMessage_100_ArriveHall` in `phonemessage_c03_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_2_AnswerPhone_MainHall true (`leveldesign/fsm/chapter3/chapter3_2/phonemessage_c03_2.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=c03_2_AnswerPhone_MainHall (`leveldesign/fsm/chapter3/chapter3_2/levelfsm_c03_2.scn.20`) |
| Likely | 153a48ad-6647-44da-8d45-3917f8753a25 | c03_2_ShotGunGet | Boolean | False | Main house and processing-area flow: marks obtaining the shotgun. | Written by `400_SetBool_ShotGunGet` in `common_c03_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_2_ShotGunGet true (`leveldesign/fsm/chapter3/common_c03_2.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_2_ShotGunGet (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 429bad7e-3448-45d5-9a61-789ea086abd0 | c03_2_ChainSaw_Interacted | Boolean | False | Main house and processing-area flow: marks picking up/interacting with the chainsaw for the Jack 2 fight. | Written by `600_Setbool_Chainsaw_Interacted` in `levelfsm_c03_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_2_ChainSaw_Interacted true (`leveldesign/fsm/chapter3/chapter3_2/levelfsm_c03_2.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=c03_2_ChainSaw_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | c1483643-0e92-4ecc-a0c5-b536e9caf5a4 | c03_2_ShowScissorsJackCore | Boolean | False | Main house and processing-area flow: controls the tutorial/visibility state for Jack 2's exposed weak point. | Read by `Tutorial_ChainSawAttack_CoreAppear` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_2_ShowScissorsJackCore (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |

### c03_4_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 5ebe635e-3b91-4fcd-be23-2473c63066d9 | c03_4_Main_3-4Start | Boolean | False | Testing-area/Lucas flow: marks the start of the Testing Area/Lucas chapter. | Written by `0_Main_StartGame` in `common_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_3-4Start true (`leveldesign/fsm/chapter3/common_c03_4.scn.20`); Gate package `c03_3_main_end.user.2` |
| Likely | 426aa5a2-35d7-4e34-8389-c12f1bd1977c | c03_4_Main_EnterMainHall | Boolean | False | Testing-area/Lucas flow: marks returning to the main hall with the snake-key route available. | Written by `25_EnterMainHall` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_EnterMainHall true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_40.user.2` |
| Confirmed | 3ceeead9-da86-48c2-89ed-73edeab7fdbf | c03_4_Main_SnakeKeyGet | Boolean | False | Snake key has been obtained. | Set by `40_SnakeKeyGet`. | `levelfsm_c03_4.scn.20`: `ItemTest` checks `MasterKey`, then `SetBool`. |
| Likely | 7af360b0-8a2e-4062-9b68-fab3d1c2811c | c03_4_Main_EventChairInteract | Boolean | False | Testing-area/Lucas flow: marks interacting with the chair/TV setup for Lucas' message. | Written by `100_InteractSofa` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_EventChairInteract true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_200.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4_Main_EventChairInteract (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | ac0ad785-77a3-4005-b6cf-12212c267c09 | c03_4_Main_LucasMessageEventEnd | Boolean | False | Testing-area/Lucas flow: marks the end of Lucas' trailer-message event. | Written by `200_LucasMesageEventEnd` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_LucasMessageEventEnd true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_300.user.2` |
| Likely | fb090a50-7b99-4248-a555-adc4597e0f78 | c03_4_Main_AnsyouBangouInteract | Boolean | False | Testing-area/Lucas flow: marks interacting with the party-room passcode keypad. | Written by `300_AnsyouBangouInteract` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_AnsyouBangouInteract true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_400.user.2` |
| Likely | 441d03ab-19e9-4e5b-b054-1c033f033730 | c03_4_Main_LucasAppearEventEnd | Boolean | False | Testing-area/Lucas flow: marks the end of Lucas' appearance/passcode trap event. | Written by `400_LucasAppearEventEnd` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_LucasAppearEventEnd true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4_Main_LucasAppearEventEnd (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Confirmed | 97ebebd9-44cb-4151-a76b-cf8772fdce27 | c03_4_Main_MoldeadRushEventStart | Boolean | False | Molded rush event has started. | Set by `500_MoldedRushEventStart`. | `levelfsm_c03_4.scn.20`: trigger `SetBool` writes true. |
| Likely | 0654561e-c103-4fd3-9ce9-9a1de7ed7074 | c03_4_Main_FallGyuusya1F | Boolean | False | Testing-area/Lucas flow: marks dropping into the barn/stable 1F. | Written by `600_FallGyuusya1F` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_FallGyuusya1F true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_700.user.2` |
| Likely | 2a44afee-cf8a-44dc-b50f-e092af65c12e | c03_4_Main_BatterySet | Boolean | False | Testing-area/Lucas flow: marks inserting the battery for the barn elevator/fat Molded setup. | Written by `700_BatterySet` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_BatterySet true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_800.user.2` |
| Likely | 73234e13-b2ff-485d-9296-c4b4d697c130 | c03_4_Main_5sec_Wait_5sec | Boolean | False | Testing-area/Lucas flow: marks the first timed wait during the barn lock-in sequence. | Written by `800_5sec_Wait(5sec)` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_5SecondsRoujyousen true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_805.user.2` |
| Likely | 352f7422-c171-416a-b934-aee16ecaa204 | c03_4_Main_13sec_Wait_13sec | Boolean | False | Testing-area/Lucas flow: marks the second timed wait during the barn lock-in sequence. | Written by `805_13sec_Wait(13sec)` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_10SecondsRoujyousen true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_810.user.2` |
| Likely | 74e4b0b6-1ceb-403f-87d6-ca1d21d08e2e | c03_4_Main_10sec_Wait_20sec | Boolean | False | Testing-area/Lucas flow: marks the 20-second point in the barn lock-in sequence. | Written by `810_10sec_Wait(20sec)` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_20SecondsRoujyousen true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_815.user.2` |
| Confirmed | 1196b63a-30ba-4a2c-bf9b-d7627a83277c | c03_4_Main_FatMoldKill | Boolean | False | Fat molded encounter kill marker. | Set by `815_FatMoldKill`. | `levelfsm_c03_4.scn.20`: `Fsm_FatMoldedCheckDead.fsm` uses `EnemyCheckDead`, then `SetBool`. |
| Likely | 844a22bd-e45a-4606-b1e3-a439c9120a21 | c03_4_Main_5sec_Wait_205sec | Boolean | False | Testing-area/Lucas flow: marks the late timed wait before the barn route opens. | Written by `835_5sec_Wait(205sec)` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_380SecondsPassedRoujyou true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_900.user.2` |
| Likely | cbb3bfb5-0af7-4718-9560-d32377e37c04 | c03_4_Main_ElectricStairsDown | Boolean | False | Testing-area/Lucas flow: marks the electric stairs/lift moving down after the barn fight. | Written by `900_ElectricStairsDown` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_ElectricStairsDown true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_1000.user.2` |
| Likely | 503e8e3e-b014-4c84-bf35-8b963e5a51e1 | c03_4_Main_LucasEventStart | Boolean | False | Testing-area/Lucas flow: marks the start of the Lucas party-room event. | Required by game-flow package `c03_4_main_1020.user.2`. | Gate package `c03_4_main_1020.user.2` |
| Likely | e8f18a82-943a-41dd-977b-f1d729499dee | c03_4_Main_PazzleRoom_CandleOn | Boolean | False | Testing-area/Lucas flow: marks receiving/lighting the candle in the party-room puzzle. | Written by `1020_PazzleRoom_CandleOn` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_DoorLock true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4_main_1030.user.2` |
| Likely | 2ae7ae0a-73c8-4c66-a2bb-cdbd45252dce | c03_4_Main_BirthDayCakeRoomEnter | Boolean | False | Testing-area/Lucas flow: marks entering the birthday cake room. | Written by `1030_BirthDayCakeRoomEnter` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_LightOn_Room true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4_main_1040.user.2` |
| Likely | 4e6c8219-6fbc-423b-b22d-8f7fa939508a | c03_4_Main_PazzleRoom_ValvePutIn | Boolean | False | Testing-area/Lucas flow: marks inserting the valve handle in the party-room puzzle. | Written by `0_ValvePutIn` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_ValvePutIn true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4c_main_100.user.2` |
| Likely | 48927745-ca9e-4298-9f81-fc590e5727bf | c03_4_Main_PazzleRoom_ToLightCandle | Boolean | False | Testing-area/Lucas flow: marks the step where the player can relight the candle. | Written by `100_ToLightCandle` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_ToLightCandle true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4_main_1060.user.2` |
| Likely | 42eb3e71-9355-4066-a82d-e44d3a4e7580 | c03_4_Main_PazzleRoom_CandleSetCake | Boolean | False | Testing-area/Lucas flow: marks putting the candle on the birthday cake. | Written by `1040_CandleSetCake` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_CandleSetCake true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4_main_1050.user.2` |
| Likely | ebf16279-dbf6-4fa9-b8fb-5dca41c1b1e5 | c03_4_Main_PazzleRoom_Clear | Boolean | False | Testing-area/Lucas flow: marks clearing Lucas' party-room puzzle safely. | Written by `1050_Ck_Flag_bool` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_Clear true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4_main_1070.user.2` |
| Likely | 74080453-17c3-4621-b728-0e777f7d0c09 | c03_4_Main_PazzleRoom_Failed | Boolean | False | Testing-area/Lucas flow: marks the failed party-room puzzle branch. | Written by `250_Ck_Flag_bool` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_Failed true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4c_main_300.user.2` |
| Likely | 626294f2-dee6-4f24-ad64-32a4f0be260d | c03_4_Main_PazzleRoom_BurstCakeEvent | Boolean | False | Testing-area/Lucas flow: marks the exploding-cake event in the party room. | Written by `1040_BurstCakeEvent` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_BurstCakeEvent true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4_main_1080.user.2` |
| Likely | 94b43af8-7a68-490a-baf4-90419dfbc5cd | c03_4_Main_PazzleRoom_PlDeadEvent | Boolean | False | Testing-area/Lucas flow: marks the player-death event in the party room. | Written by `400_PlDeadEvent` in `puzzle_advanced_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_PazzleRoom_PlDeadEvent true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`); Gate package `c03_4c_main_end.user.2` |
| Unknown | c12f9f3e-e296-4e85-86d1-b93734d5b019 | c03_4_Main_CakeBombInteract | Boolean | False |  |  |  |
| Likely | 4f566806-6f95-49b4-8e4b-ff951ee8fd68 | c03_4_Main_CakeBomb_DoorOpen | Boolean | False | Testing-area/Lucas flow: marks the post-bomb party-room door opening. | Written by `1000_PazzleDoorUnLock_FFS` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_BirthdayPazzle2End true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_1300.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4_Main_CakeBomb_DoorOpen (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 8c068ded-d0e7-42d0-8c57-56a0a3f2a1bb | c03_4_Main_BackYardpassThrough | Boolean | False | Testing-area/Lucas flow: marks passing through the backyard after Lucas' area. | Written by `1300_BackYardPassThrough` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_Main_BackYardpassThrough true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`); Gate package `c03_4_main_end.user.2` |
| Unknown | 6c22fd64-8b0b-4667-b57c-bd0331bed15f | c03_4_Main_Lucas_EventEnd | Boolean | False |  |  |  |
| Unknown | eccd86f3-bf8e-4b67-95f2-7275b47a0a3a | c03_4_Main_Lucas_CloseDoor_Trg | Boolean | False |  |  |  |

### c03_4_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 211131dc-2572-4c16-9f88-50e6c4abc15e | Valve_Get_PRoom2 | Boolean | False |  |  |  |
| Unknown | fa639ab8-f8df-4559-93dd-22a403bb7cd0 | Puppet_Not_Setup | Boolean | False |  |  |  |
| Likely | ca0f3498-d912-43a7-a2d5-8160ca32adce | Password_A_Used | Boolean | False | Testing-area/Lucas flow: tracks that password attempt A has been used at Lucas' keypad. | Read by `900_PassCoadGimmick` in `levelfsm_c03_4.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Password_A_Used (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`) |
| Likely | fe7cb3ab-5ef0-4d87-b4bc-aa3a134f39a7 | Password_B_Used | Boolean | False | Testing-area/Lucas flow: tracks that password attempt B has been used at Lucas' keypad. | Read by `900_PassCoadGimmick` in `levelfsm_c03_4.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Password_B_Used (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`) |
| Unknown | da0e2a65-1052-4554-ba00-1d030423c3fc | Password_C_Used | Boolean | False |  |  |  |
| Likely | f07d9462-d4ab-4fa9-86dd-cea13aca4965 | CakeBombBoX_Open | Trigger | 0 | Testing-area/Lucas flow: Trigger/event pulse for opening the cake-bomb box. | Read by `c03_4_MainFlow` in `common_c03_4.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=CakeBombBoX_Open (`leveldesign/fsm/chapter3/common_c03_4.scn.20`) |
| Likely | 30fed3bf-5b02-4fdd-b96d-433da5e8874d | BumbTimeSafe | Boolean | False | Testing-area/Lucas flow: tracks the safe/defused timer branch for the party-room bomb. | Written by `1280_BunbTimeSafe_SetBool` in `puzzle_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets BumbTimeSafe true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=BumbTimeSafe (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`) |
| Likely | 11687bfb-4035-428b-b54f-406283e58949 | c03_4_AnswerPhone_FromLucas | Boolean | False | Testing-area/Lucas flow: tracks Lucas' trailer-house phone call. | Written by `PhoneMessage_100_TrailerHouseFromLucas` in `phonemessage_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_AnswerPhone_FromLucas true (`leveldesign/fsm/chapter3/chapter3_4/phonemessage_c03_4.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4_AnswerPhone_FromLucas (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`) |
| Likely | b893db0f-370a-487a-be56-624f0a3891b3 | OlicanInteract | Boolean | False | Testing-area/Lucas flow: tracks interacting with the oil can in the party-room puzzle. | Written by `1020_OilCan_SetBool` in `puzzle_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets OlicanInteract false (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=OlicanInteract (`leveldesign/fsm/chapter3/chapter3_4/puzzle_advanced_gimmick.scn.20`) |
| Likely | 69106612-6721-4c33-bc84-9c933a46317a | BurnPazzleItem | Boolean | False | Testing-area/Lucas flow: tracks burning/losing the puzzle item during the party-room sequence. | Written by `1300_SekikomuMes_ItemLost_SetBool` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets BurnPazzleItem true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`) |
| Likely | 9e8a7383-afc2-4167-bb9b-5f61f6b80e96 | c03_4_AnswerPhone_FromLucasInHall | Boolean | False | Testing-area/Lucas flow: tracks Lucas' main-hall phone call. | Written by `PhoneMessage_200_MainHallFromLucas` in `phonemessage_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4_AnswerPhone_FromLucasInHall true (`leveldesign/fsm/chapter3/chapter3_4/phonemessage_c03_4.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4_AnswerPhone_FromLucasInHall (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`) |
| Likely | 1da9d896-a008-4f9d-aa30-5b49de657d7c | LastCandleFire | Boolean | False | Testing-area/Lucas flow: tracks the final candle flame state in the party-room puzzle. | Written by `1040_LasrCandleFireFlag` in `puzzle_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets LastCandleFire true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=LastCandleFire (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`) |

### c03_4A_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 01da685b-cd8a-404b-8ca3-8c738145bdae | c03_4A_Main_LadderTakeDown | Boolean | False | Main house optional-card-key branch: marks lowering the attic ladder in the main-house card-key branch. | Written by `0_TrophyInteract` in `levelfsm_c03_4a.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4Main_LadderTakeDown true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4a.scn.20`); Gate package `c03_4a_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4A_Main_LadderTakeDown (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | b5532ea8-facb-428d-bf61-ea3e66d373dd | c03_4A_Main_LucasCardKeyGet_InLoft | Boolean | False | Main house optional-card-key branch: marks obtaining Lucas' card key in the loft. | Written by `100_Main_LucasCardKeyGet_InLoft` in `levelfsm_c03_4a.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4A_Main_LucasCardKeyGet_InLoft true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4a.scn.20`); Gate package `c03_4_main_100.user.2` |

### c03_4A_Flag

_No variables were listed under this group in the captured baseline log._

### c03_4B_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 766197a6-59fb-49d5-b631-f0e4c6b789a4 | c03_4B_Main_BedNumberUnlocking | Boolean | False | Dissection-room optional-card-key branch: marks unlocking the bed-number mechanism in the dissection-room branch. | Written by `0_BedNumberUnLock` in `levelfsm_c03_4b.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4B_Main_BedNumberUnlocking true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4b.scn.20`); Gate package `c03_4b_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_4B_Main_BedNumberUnlocking (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | b9f9b409-c6b8-4142-9697-c70f24a7c15b | c03_4B_Main_LucasCardKeyGet_InWorkRoom | Boolean | False | Dissection-room optional-card-key branch: marks obtaining Lucas' card key in the workshop. | Written by `100_Main_LucasCardKeyGet_InWorkRoom` in `levelfsm_c03_4b.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_4B_Main_LucasCardKeyGet_InWorkRoom true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4b.scn.20`); Gate package `c03_4_main_100.user.2` |

### c03_4B_Flag

_No variables were listed under this group in the captured baseline log._

### FF040_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | bb274bcc-40b9-4b0c-ad75-113d979acbf9 | FF040_Main_FirstEvent_End | Boolean | False | Happy Birthday videotape flow: marks the end of the Happy Birthday tape opening event. | Written by `0_First_EventEnd` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_FirstEvent_End true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_50.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF040_Main_FirstEvent_End (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | 4f097504-ec18-4973-a07c-9a482a2ba386 | FF040_Main_Candle_Interact | Boolean | False | Happy Birthday videotape flow: marks taking/interacting with the candle in the Happy Birthday tape. | Written by `50_Candle_Interact` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_Candle_Interact true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_100.user.2` |
| Likely | 7d3298f5-4f4e-4049-b495-3f85a9ef3a4b | FF040_Main_LightOn_Room | Boolean | False | Happy Birthday videotape flow: marks turning on/restoring light in the Happy Birthday puzzle room. | Written by `100_Message_Light_ON` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_LightOn_Room true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_200.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF040_Main_LightOn_Room (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | 5cb1f17c-5672-4350-880d-2f9f9d81f9a1 | FF040_Main_ValvePutIn | Boolean | False | Happy Birthday videotape flow: marks inserting the valve handle in the Happy Birthday tape. | Written by `200_ValvePutIn` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_ValvePutIn true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_300.user.2` |
| Likely | 0e701974-b51d-4312-8e5f-663e211d2a79 | FF040_Main_ToLightCandle | Boolean | False | Happy Birthday videotape flow: marks the step where Clancy can relight the candle. | Written by `300_ToLightCandle` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_ToLightCandle true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_400.user.2` |
| Likely | 52740c78-4274-4e18-9f71-a6e58fd2e4ac | FF040_Main_CandleSetCake | Boolean | False | Happy Birthday videotape flow: marks putting the candle on the birthday cake. | Written by `400_CandleSetCake` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_CandleSetCake true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF040_Main_CandleSetCake (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | c5db409e-163b-4156-bf72-375b009316a2 | FF040_Main_BurstCakeEvent | Boolean | False | Happy Birthday videotape flow: marks the exploding-cake event in the Happy Birthday tape. | Written by `500_BurstCakeEvent` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_BurstCakeEvent true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_600.user.2` |
| Likely | 03742627-a56f-4907-a177-35eee2a7c6ac | FF040_Main_ClancyDeadEvent | Boolean | False | Happy Birthday videotape flow: marks Clancy's death event in the Happy Birthday tape. | Written by `600_ClancyDeadEvent` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_ClancyDeadEvent true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_700.user.2` |
| Likely | e640b41b-d05f-46a7-8b9e-5b6b0ca2c94b | FF040_Main_AfterRequestJump | Boolean | False | Happy Birthday videotape flow: marks the end/jump out of the Happy Birthday tape. | Written by `700_RequestJump` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF040_Main_AfterRequestJump true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Gate package `ff040_main_end.user.2` |

### FF040_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | b0ef4d28-823b-4920-b4f0-33d6bb275d39 | Get_TV_Value | Boolean | False | Happy Birthday videotape flow: tracks the TV/code value clue in the Happy Birthday puzzle. | Read by `1040_SignLock` in `puzzle_gimmick.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Get_TV_Value (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`) |
| Unknown | cd0aa33d-c4cc-4944-b1f4-b6dace6196b7 | Get_FF040Coin | Boolean | False |  |  |  |
| Likely | da17809f-6d10-4a0f-b482-e159ceb59422 | LastCandleFire_FF040 | Boolean | False | Happy Birthday videotape flow: tracks the final candle flame state in the Happy Birthday tape. | Written by `0_Flag_LastCandoleFireFF040Contorol` in `levelfsm_ff040.scn.20`. | Write via `via.fsm.action.SetBool`: sets LastCandleFire_FF040 true (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=LastCandleFire_FF040 (`leveldesign/fsm/ff040/levelfsm_ff040.scn.20`) |

### Combine_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 38208fea-638c-4d54-ac9c-8d05a31436dd | cmb_releasable_RemedyS | Boolean | False |  |  |  |
| Unknown | d5c61cc1-5fc3-42bd-a247-a0673c3dc1b8 | cmb_enable_RemedyS | Boolean | False |  |  |  |
| Unknown | d8e59fe1-a257-4a78-8574-d20f5ad35e1d | cmb_enable_Eye | Boolean | False |  |  |  |
| Unknown | 66203bf4-f916-42bc-af44-05990479f5e6 | cmb_enable_Fuel | Boolean | False |  |  |  |
| Unknown | 5135aae4-9684-45b3-bd37-9edfbceaf054 | cmb_enable_Gunpowder | Boolean | False |  |  |  |
| Unknown | c4256fad-6e47-44e7-9736-54868dfd4214 | cmb_enable_Strength | Boolean | False |  |  |  |
| Unknown | 2d37e99b-b701-4f78-b0a7-1c7c7bc3df68 | cmb_enable_Sparekey | Boolean | False |  |  |  |
| Unknown | 79aba106-1ebe-44ca-93f3-9ee34b118137 | cmb_enable_Plasticexplosive | Boolean | False |  |  |  |
| Unknown | eab670f7-4475-4258-b900-cbe91664c9a3 | cmb_enable_DybbukMedicine | Boolean | False |  |  |  |

### c03_5_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Confirmed | 7cd09cfb-5f30-4ab9-bac4-9036f66d78f6 | c03_5_Main_Chapter3_5Start | Boolean | False | Chapter 3_5 start marker. | Set by `0_Main_Start_AreaIn`. | `levelfsm_c03_5.scn.20`: `TempFsm_TriggerInAction_SetBool` writes this GUID true. Gates `c03_5_main_100.user.2`. |
| Confirmed | 9d7892e2-a7cf-4c50-8856-c8c8a72a0312 | c03_5_Main_2tumeClankMawasita | Boolean | False | Second crank/bridge progression marker. | Set by `100_2tumekclankMawashita`. | `levelfsm_c03_5.scn.20`: trigger `SetBool` writes true. Gates `c03_5_main_150.user.2`. |
| Confirmed | 4dec615b-7bc4-48ec-a67f-d3565252e7dc | c03_5_Main_EnterTheBoatHouse | Boolean | False | Player has entered the boat house. | Set by `150_Main_EnterTheBoatHouse`. | `levelfsm_c03_5.scn.20`: trigger `SetBool` writes true. Gates `c03_5_main_175.user.2`. |
| Confirmed | 4f1a321e-06b1-42ee-8820-699f2d8c1220 | c03_5_Main_TalkWithCapturedZoeEventEnd | Boolean | False | Captured Zoe conversation/event ended. | Set by `175_Main_KousokuEventStart`. | `levelfsm_c03_5.scn.20`: `WaitNPCEventEnd` followed by `SetBool`. Gates `c03_5_main_200.user.2`. |
| Confirmed | f33b4bbe-50d3-4b58-8acf-3269de2d2679 | c03_5_Main_Give2SerumMaterialsFinished | Boolean | False | Both serum materials have been given to Zoe. | Set by `200_Main_Give2SerumMaterialsFinished`. | `levelfsm_c03_5.scn.20`: `FsmStateCheck` on Zoe rescue/event FSM, then `SetBool`. Gates `c03_5_main_250.user.2`. |
| Confirmed | 5b704250-3aeb-4551-b935-a9123e7a72fe | c03_5_Main_SerumMakeEventEnd | Boolean | False | Serum creation event ended. | Set by `250_Main_SerumMakeEventEnd`; manual-save helper can also set it. | `levelfsm_c03_5.scn.20`: `WaitNPCEventEnd_SetBool` and `250_Main_SerrumMakeEventEnd_ForManualSave`. Gates `c03_5_main_300.user.2`. |
| Confirmed | 2f3f5ec1-c595-4078-a686-118a5a8d1a8f | c03_5_Main_GetKesseiEventEnd | Boolean | False | Serum pickup/event ended before Jack 3 setup. | Set by `300_Main_GetKesseiEvent_End`. | `levelfsm_c03_5.scn.20`: `FsmStateCheck` waits for `SuccessEnd`, then `SetBool`. Gates `c03_5_main_400.user.2`. |
| Confirmed | 2b6fa797-a24d-4604-bffc-b991aec1d004 | c03_5_Main_JackGrabEventEnd | Boolean | False | Jack 3 battle-start grab event has ended; safe "battle started" gate. | Set by `400_Main_JackGrabEvent_End` after `400_Jack_BattleStart` reaches `StateEnd`. | `levelfsm_c03_5.scn.20`: `FsmStateCheck` targets `400_Jack_BattleStart`, then `SetBool`. Gates `c03_5_main_500.user.2`. |
| Confirmed | 95cc0879-5f60-4517-b752-54cf79a4782b | c03_5_Main_MukadeJackBattleEnd | Boolean | False | Jack 3/Mukade battle is over. | Set by `500_Main_MukadeFight_End`. | `levelfsm_c03_5.scn.20`: `app.WaitEmBattleEnd` followed by `SetBool`. Gates `c03_5_main_600.user.2`. |
| Confirmed | c1bf483a-19b2-4f69-84c9-6fd5663c1b95 | c03_5_Main_SanbasiDoorOpenEventEnd | Boolean | False | Pier/boat house door open event ended. | Set by `600_Main_ZoiDoorOpenEvent_End`. | `levelfsm_c03_5.scn.20`: `WaitNPCEventEnd_SetBool`. Gates `c03_5_main_700.user.2`. |
| Confirmed | 9b41dd8e-60af-43e1-9be6-fc978232e42c | c03_5_Main_StartJackGrabLoopEvent | Boolean | False | Final Jack grab loop event started. | Set by `700_Main_StartJackGrabLoopEvent`. | `levelfsm_c03_5.scn.20`: main-flow object writes this flag; gates `c03_5_main_750.user.2`. |
| Confirmed | 43a9b4bb-8b34-49f4-b5bd-8a78cb612134 | c03_5_Main_JackTodomeEventEnd | Boolean | False | Final Jack finishing event ended. | Set by the `750` Jack finish event object. | `levelfsm_c03_5.scn.20`: main-flow `SetBool`; gates `c03_5_main_800.user.2`. |
| Confirmed | bd5595c5-7484-4a27-9be8-79068d2b6b68 | c03_5_Main_GoToPier | Boolean | False | Player has gone to the pier. | Set by `800_Main_GoToPier`. | `levelfsm_c03_5.scn.20`: trigger `SetBool`; gates `c03_5_main_900.user.2`. |
| Confirmed | 4808a3ca-3497-4090-9a3d-3cd8345d3755 | c03_5_Main_PreChoiseEvent_End | Boolean | False | Pre-choice event ended. | Set by `900_PreChoiseStart_EventEnd`. | `levelfsm_c03_5.scn.20`: main-flow event end marker; gates `c03_5_main_1000.user.2`. |
| Confirmed | 8e01f789-4f80-4524-a7b7-4f35496724ca | c03_5_Main_WaitChoiceEventLoop | Boolean | False | Choice wait loop reached. | Set by `1000_WaitChoiseEventEnd`. | `levelfsm_c03_5.scn.20`: choice-wait flow marker; gates `c03_5_main_1200.user.2`. |
| Confirmed | 8d1648e7-c640-407c-9413-a5b109b05459 | c03_5_Main_BoatEscapeEventEnd | Boolean | False | Boat escape event ended; c03_5 main flow can end. | Set by `1200_Main_ChoiceEvent_Mia_End` or `1200_Main_ChoiceEvent_Zoe_End`. | `levelfsm_c03_5.scn.20`: choice-end objects write this flag. Gates `c03_5_main_end.user.2`. |

### c03_5_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Confirmed | ad0f3d3f-89fe-47a5-b54e-881a35341ed2 | Choice_Mia | Boolean | True | Mia/Zoe branch choice; true means Mia, false means Zoe. | `1200_MiaChoice` writes true; `1200_ZoiChoice` writes false. | `levelfsm_c03_5.scn.20`: both choice FSMs write the same GUID with opposite `SetBool.Status` values. |
| Likely | 080e89b3-31c8-4c2e-ad76-8f5dc06610b0 | C03_5_ZoeComplainRandom | Int32 | 0 | Boat house finale flow: stores Zoe's random complaint/message branch while waiting for serum materials. | Written by `200_ZoeRescuedEvent2ndTime` in `levelfsm_c03_5.scn.20`. | Write via `app.fsm.RandomFlagSet`: sets random int 0..3 (`leveldesign/fsm/chapter3/chapter3_5/levelfsm_c03_5.scn.20`) |
| Confirmed | 9435290b-97b5-4852-a656-8641ad5f72c3 | C03_5_HeardNoSerumMaterialMessage | Boolean | False | Tracks whether Zoe's no-serum-material message has already been shown. | Set by `Fsm_ZoeRescuedEvent2ndTime` after the message branch. | `levelfsm_c03_5.scn.20`: same FSM has `ConditionUserDataExpression` with `GlobalUserData` true and false checks against this GUID, then writes it true after messages. |
| Likely | 14dfeb12-f1ce-42ad-80f4-21dacb1072ad | C03_5_MiaRescued | Boolean | False | Boat house finale flow: marks that Mia's rescue event in the boat house has been played. | Written by `175_MiaRescuedEvent_SetBool_FFS` in `levelfsm_c03_5.scn.20`. | Write via `via.fsm.action.SetBool`: sets C03_5_MiaRescued true (`leveldesign/fsm/chapter3/chapter3_5/levelfsm_c03_5.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=C03_5_MiaRescued (`leveldesign/fsm/chapter3/chapter3_5/levelfsm_c03_5.scn.20`) |

### c123_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | f8100843-b7ad-4e04-8dfd-8f831f2bd397 | c123_Main_Cp1FinalEventEnd | Boolean | False | Shared story-transition flow: marks the end of the chapter 1 final transition event. | Written by `0_Wait_Cp1FinalEventEnd` in `levelfsm_c123.scn.20`. | Write via `via.fsm.action.SetBool`: sets c123_Main_Cp1FinalEventEnd true (`leveldesign/fsm/chapter123/levelfsm_c123.scn.20`); Gate package `c123_main_100.user.2` |
| Likely | 7c5ad169-add6-4409-870a-6c04427ad325 | c123_Main_Cp3FirstEventEnd | Boolean | False | Shared story-transition flow: marks the end of the chapter 3 first transition event. | Written by `100_Wait_Cp3FirstEventEnd` in `levelfsm_c123.scn.20`. | Write via `via.fsm.action.SetBool`: sets c123_Main_Cp3FirstEventEnd true (`leveldesign/fsm/chapter123/levelfsm_c123.scn.20`); Gate package `c123_main_200.user.2` |

### c123_Flag

_No variables were listed under this group in the captured baseline log._

### c04_1_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Confirmed | 0dae1ecb-d230-4d3d-b879-b4e1896224a7 | C04_1_Main_MiaEventEnd | Boolean | False | Mia opening event on the ship has ended. | Set by `0_MiaEventEnd`. | `levelfsm_c04_1.scn.20`: `WaitNPCEventEnd_SetBool` writes this GUID true. |
| Likely | 01de4aeb-3d77-4ff2-89ea-38adaac43856 | C04_1_Main_FlashBack1EventLineA | Boolean | False | Wrecked ship Mia flow: marks the first Eveline flashback line/trigger on the wrecked ship. | Written by `25_FlashuBack1TriggerEnter` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets C04_1_Main_FlashBack1EventLineA true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_50.user.2` |
| Likely | 15b905fb-525c-4310-9b0f-c18f2e6f25ff | c04_1_WorkshopRoomEnter | Boolean | False | Wrecked ship Mia flow: marks entering the ship workshop room. | Written by `50_WorkshopRoomEnter` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_WorkshopRoomEnter true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_100.user.2` |
| Likely | 63cfc89b-3731-4ee3-a85a-e59a78b8b167 | c04_1_Main_FlashBack3EventLineD | Boolean | False | Wrecked ship Mia flow: marks the third Eveline flashback line/trigger on the wrecked ship. | Written by `100_FlashBack3_TriggerEnter` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_FlashBack3EventLineD true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_200.user.2` |
| Confirmed | 39b1359b-905a-4cf1-bd09-d813bc9e67cf | c04_1_Main_EvelynDoorCloseEventEnd | Boolean | False | Eveline door-close event has ended. | Set by `200_EvelynDoorCloseEventEnd`. | `levelfsm_c04_1.scn.20`: `FsmStateCheck` waits for `StateEnd`, then `SetBool`. |
| Confirmed | 1e48eea7-1cef-4b14-8c12-0b7cb9920832 | c04_1_Main_3FSecurityRoomEnter | Boolean | False | Mia has entered the 3F security room. | Set by `210_SecurityRoomEnter`. | `levelfsm_c04_1.scn.20`: trigger `SetBool` writes true. |
| Likely | 4ccc6408-ec00-4ad9-9ad4-019fa227526d | c04_1_Main_VideoTapeInDeck | Boolean | False | Wrecked ship Mia flow: marks inserting/starting the old videotape on the ship. | Written by `300_VideoTapeInDeck` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_VideoTapeInDeck true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_350.user.2` |
| Likely | 955465a4-fc9e-4fbd-b8d0-d997d0ee490f | c04_1_Main_VideoPlayFF | Boolean | False | Wrecked ship Mia flow: marks playback of the ship old videotape. | Written by `800_c04_MainFlag_ON` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_VideoPlayFF true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `c04_01_main_400.user.2` |
| Likely | 8a4e3652-ab64-4180-bf39-42668534be3b | c04_1_Main_VideoEvelynEventEnd | Boolean | False | Wrecked ship Mia flow: marks the end of the Eveline event after the old videotape. | Written by `400_VideoEvelynEventEnd` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_VideoEvelynEventEnd true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_1_Main_VideoEvelynEventEnd (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | a5ce782e-f518-48fb-a27a-09cf22a5fc59 | c04_1_Main_ElevatorGetInside | Boolean | False | Wrecked ship Mia flow: marks getting inside the ship elevator. | Written by `500_ElevatorGetInside` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_ElevatorGetInside true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_600.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_1_Main_ElevatorGetInside (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | c44b2beb-34c1-4547-99ab-20ab9694da43 | c04_1_Main_ElevatorMove | Boolean | False | Wrecked ship Mia flow: marks the ship elevator movement sequence. | Written by `600_ElevatorMove` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_ElevatorMove true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_800.user.2` |
| Likely | 219d0e3a-5847-4a96-88b3-70c2fb61e1f4 | c04_1_Main_B2AreaEnter | Boolean | False | Wrecked ship Mia flow: marks entering the ship B2 area. | Written by `800_B2AreaEnter` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_B2AreaEnter true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_900.user.2` |
| Likely | 0ca972a8-0131-4284-8ad6-0a1699b68362 | c04_1_Main_EvelineEventEnd | Boolean | False | Wrecked ship Mia flow: marks the end of the engine-room Eveline event. | Written by `900_EngineRoomEvelinEventEnd` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_EvelineEventEnd true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_950.user.2` |
| Likely | 009bee9a-5c9f-49b7-abd7-47210d48178e | c04_1_Main_HelpOutEthanStart | Boolean | False | Wrecked ship Mia flow: marks the start of Mia helping Ethan. | Written by `900_helpOutEthanStart` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_HelpOutEthanStart true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_1000.user.2` |
| Likely | 75171c12-4dc5-49d4-895b-a1fe72273702 | c04_1_Main_HelpOutEthanEnd | Boolean | False | Wrecked ship Mia flow: marks the end of Mia helping Ethan and the transition to Ethan. | Written by `1000_EventFadeOut` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_1_Main_HelpOutEthanEnd true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_01_main_end.user.2` |

### c04_1_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 462cc0d3-c9f9-4914-807e-71a731145674 | c04_1_FirstLook | Boolean | False |  |  |  |
| Unknown | cd80fa84-1130-4378-9edf-9ef08cbe0488 | c04_1_FatFirstLiquid | Boolean | False |  |  |  |
| Unknown | d0b91561-cf14-4d69-8f9a-7bb1e4364136 | c04_1_PlMiaHeadModel | Boolean | False |  |  |  |
| Unknown | bbc6e01e-2ac6-43d2-8807-2e1362851271 | c04_1_PCChannel01 | Boolean | False |  |  |  |
| Unknown | 5fe2c65f-f2e2-44e8-aea1-6c26acebfa2b | c04_1_PCChannel02 | Boolean | False |  |  |  |
| Unknown | 06fc2bc0-bf85-463a-bcbd-bea4650d1bde | c04_1_PCChannel03 | Boolean | False |  |  |  |
| Unknown | 4dcd6edd-4de8-42e5-bc03-0e1f420fa35f | c04_1_PCChannel04 | Boolean | False |  |  |  |
| Likely | cc0f0fdd-1b52-4872-9546-6ca9b6fffabe | c04_1_MiaArmHoldHide | Boolean | False | Wrecked ship Mia flow: controls hiding Mia's arm-hold model/state on the ship. | Read by `500_BridgeEvelinBGM` in `levelfsm_c04_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_1_MiaArmHoldHide (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`) |

### c04_2_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | c1747ccd-72b6-4531-a813-2de127e60e77 | c04_2_Main_Chap4-2EventStart | Boolean | False | Wrecked ship rescue/mines approach flow: marks the start of the post-ship Ethan rescue/mines approach section. | Written by `1000_EventFadeOut` in `levelfsm_c04_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_Chap4-2EventStart true (`leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20`); Gate package `c04_2_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_2_Main_Chap4-2EventStart (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`) |
| Likely | 5de0d07b-46a6-4c0a-8c4e-94fba42803f3 | c04_2_Main_Chap4-2EventEnd | Boolean | False | Wrecked ship rescue/mines approach flow: marks the end of the c04_2 opening event. | Written by `100_Chap4-2EventEnd` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_Chap4-2EventEnd true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_150.user.2` |
| Likely | 807a2f20-9148-47c8-af58-462e19021a51 | c04_2_Main_MiaBattleEnd | Boolean | False | Wrecked ship rescue/mines approach flow: marks that the Mia/Ethan branch battle event is over. | Written by `150＿MiaBattleEnd` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_MiaBattleEnd true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_200.user.2` |
| Likely | 8420dd53-0f5f-4d24-8cc2-1dec5d713436 | c04_2_Main_RockSaltDoomLiftDown | Boolean | False | Wrecked ship rescue/mines approach flow: marks the salt-mine lift moving down. | Written by `200_RockSaltDoomLiftDown` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_RockSaltDoomLiftDown true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_300.user.2` |
| Likely | b943513c-9a7f-4ce9-bdcf-9d90082240f9 | c04_2_Main_StudyAreaEnter | Boolean | False | Wrecked ship rescue/mines approach flow: marks entering the study/safe area before the mines. | Written by `300_StudyAreaEnter` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_StudyAreaEnter true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_400.user.2` |
| Likely | c651a277-8b20-4998-a09f-0269db1cd08a | c04_2_Main_SpiralAreaEnter | Boolean | False | Wrecked ship rescue/mines approach flow: marks entering the spiral mine approach. | Written by `400_SpiralAreaEnter` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_SpiralAreaEnter true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_450.user.2` |
| Likely | 352cd6db-6fc1-4f0b-8180-be748e5d523f | c04_2_Main_EvelineHide | Boolean | False | Wrecked ship rescue/mines approach flow: controls Eveline's hide/show state on the mines approach. | Written by `450_EvelineHideSetBool` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_EvelineHide true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_500.user.2` |
| Likely | 7543dfe3-dc47-4afb-9506-79f18ae5a495 | c04_2_Main_RockSaltDoomExit | Boolean | False | Wrecked ship rescue/mines approach flow: marks exiting the salt-mine lift/approach section. | Written by `500_RockSaltDoomExit` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_2_Main_RockSaltDoomExit true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`); Gate package `c04_2_main_end.user.2` |

### c04_2_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 53fc9d2e-1025-44b5-94c9-e435c61983e4 | CrankGet | Boolean | False |  |  |  |
| Likely | 8e27e550-2a92-495f-8bc6-8553e73c62a7 | MiaItemGet | Boolean | False | Wrecked ship rescue/mines approach flow: marks receiving Mia's item handoff after the ship section. | Written by `200_ItemBoxReceive` in `levelfsm_c04_2.scn.20`. | Write via `via.fsm.action.SetBool`: sets MiaItemGet true (`leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20`) |
| Unknown | b8cb1455-69ab-44fd-83d3-8021d2ecef04 | CheckedMiaItemGet | Boolean | False |  |  |  |

### c324_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | ec71f7f1-6c96-4f4f-8956-8ddd05810636 | EndEvent | Boolean | False | Shared ending-transition flow: marks the shared boat/ending transition event. | Written by `0_Wait_BoatEvent` in `levelfsm_c324.scn.20`. | Write via `via.fsm.action.SetBool`: sets EndEvent true (`leveldesign/fsm/chapter324/levelfsm_c324.scn.20`); Gate package `c324_main_100.user.2` |

### c324_Flag

_No variables were listed under this group in the captured baseline log._

### c00_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | c4c9add3-2ff3-4683-ba7f-0ebf9018ea91 | MonologueEnd | Boolean | False | Opening/intro flow: marks the end of the opening monologue. | Written by `0_Wait_Monologue_End` in `levelfsm_c00.scn.20`. | Write via `via.fsm.action.SetBool`: sets MonologueEnd true (`leveldesign/fsm/chapter0/levelfsm_c00.scn.20`); Gate package `c00_main_50.user.2` |
| Likely | 4a054668-a6d5-4e74-997f-883342d90480 | CompFadeOut | Boolean | False | Opening/intro flow: marks completion of the opening fade-out. | Written by `100_Ck_FadeOut` in `levelfsm_c00.scn.20`. | Write via `via.fsm.action.SetBool`: sets CompFadeOut true (`leveldesign/fsm/chapter0/levelfsm_c00.scn.20`); Gate package `c00_main_end.user.2` |

### c00_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | fd365da6-a4a4-4250-b9de-26813e7c342d | c00_BreakWindow0 | Boolean | False |  |  |  |
| Unknown | b1eecc00-b66b-490f-8291-fe16c1b479e4 | c00_BreakWindow1 | Boolean | False |  |  |  |
| Unknown | 8988049d-c53a-4abb-9b11-bdcf04bce1e5 | c00_BreakWindow2 | Boolean | False |  |  |  |
| Likely | 4dd01c36-83b7-4357-8153-d384f61f3153 | c00_BurstEffect0 | Boolean | False | Opening/intro flow: tracks/reset a burst effect used by the later ship flashback flow. | Written by `900_Ev_Effect_Burst` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets c00_BurstEffect0 false (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c00_BurstEffect0 (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`) |
| Unknown | a3e79d51-f060-4ecf-a5ab-10f1f259cab7 | c00_BreakWindow00 | Boolean | False |  |  |  |
| Unknown | 71a688b6-1046-4f8b-81a6-3cc5c2799b13 | c00_BreakWindow01 | Boolean | False |  |  |  |

### c04_3_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 2f4b0280-de49-4f62-a7d1-f8af2c5fdd6d | c04_3_Main_GameStart | Boolean | False | Salt mine and ending flow: marks the start of the salt mine/finale section. | Written by `0_GameStart` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_GameStart true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_10.user.2` |
| Likely | 3f403d5f-6178-4268-b1ab-165b348bdb7d | c04_3_Main_FB1PlayEnd | Boolean | False | Salt mine and ending flow: marks the first finale flashback as finished. | Written by `10_FB1PlayEnd` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_FB1PlayEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_20.user.2` |
| Likely | c4d02eaa-4669-4230-a735-98db524e5088 | c04_3_Main_FB2PlayEnd | Boolean | False | Salt mine and ending flow: marks the second finale flashback as finished. | Written by `20_FB2PlayEnd` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_FB2PlayEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_30.user.2` |
| Likely | 28aa65ae-6e4b-4626-91df-46d9e04f86e7 | c04_3_Main_FB3PlayEnd | Boolean | False | Salt mine and ending flow: marks the third finale flashback as finished. | Written by `30_FB3PlayEnd` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_FB3PlayEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_40.user.2` |
| Likely | 8061c9db-6116-4e83-a4ba-060819ab7520 | c04_3_Main_EvelynAngerEventEndã€€ | Boolean | False | Salt mine and ending flow: marks that Eveline anger event has ended/cleared. | Written by `40_EvelynAngerEventStart` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_EvelynAngerEventEnd　 true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_50.user.2` |
| Likely | 2ea1d2d9-1f47-4bbe-88ec-d6cd4c64a246 | c04_3_Main_LastBossBattleStart | Boolean | False | Salt mine and ending flow: marks the start of the final Eveline boss battle. | Written by `50_LastBattleStart` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_LastBossBattleStart true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_3_Main_LastBossBattleStart (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 6eae91d6-84e6-4a0d-92f9-aa85323b8397 | c04_3_Main_LasBossBattleEnd | Boolean | False | Salt mine and ending flow: marks that the final Eveline boss battle is over. | Written by `100_LastBossBattleEndStateCheck` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_LasBossBattleEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_600.user.2` |
| Likely | ef276132-1470-4ddf-9f4f-16e9d9b66de9 | c04_3_Main_BranchEventEnd | Boolean | False | Salt mine and ending flow: marks the end of the ending-branch event. | Written by `600_Finish_CrisEvent` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_BranchEventEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_650.user.2` |
| Likely | 70cc9385-bd9a-430d-b611-9e9fc2001eb5 | c04_3_Main_LogoEnd | Boolean | False | Salt mine and ending flow: marks the end of the logo/title transition. | Written by `650_Finish_Logo` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_LogoEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_700.user.2` |
| Likely | 9625c3d6-7f63-4fe8-b235-1d167ee53dda | c04_3_Main_CreditEnd | Boolean | False | Salt mine and ending flow: marks the end of the credits. | Written by `700_KariWaitMovieEnd` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_CreditEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_800.user.2` |
| Likely | f0a38657-d85c-4861-967a-738607440624 | c04_3_Main_LastEventEnd | Boolean | False | Salt mine and ending flow: marks the final event as ended. | Written by `800_Finishi_LastEvent` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_3_Main_LastEventEnd true (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Gate package `c04_3_main_end.user.2` |

### c04_3_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | d1f22689-176f-4504-8499-507eb95ab697 | EpilogueToLogo | Boolean | False | Salt mine and ending flow: tracks the transition from epilogue to logo/title. | Read by `600_Finish_CrisEvent` in `levelfsm_c04_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=EpilogueToLogo (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`) |
| Unknown | d6204191-82fb-4cf0-a210-f4193232eaa3 | ZoeEpiloguePhoneMovie | Boolean | False |  |  |  |
| Likely | 9281a6fa-fadb-4131-8def-b57d5826ca34 | 3rdFbEvelynHide | Boolean | False | Salt mine and ending flow: controls Eveline's hide/show state in the third finale flashback. | Read by `30_FB_EvelynHideEff` in `levelfsm_c04_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=3rdFbEvelynHide (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`) |

### UI_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 687a8884-de6d-428f-8cdb-37822f344446 | EnableQuickSlot | Boolean | True | UI guidance state: enables enable quick slot. | Written by `1300_EnableQuickSlot_Ture` in `levelfsm_c03_4.scn.20`. | Write via `via.fsm.action.SetBool`: sets EnableQuickSlot true (`leveldesign/fsm/chapter3/chapter3_4/levelfsm_c03_4.scn.20`) |
| Unknown | b92f584f-c686-480c-9ca5-27048348efa5 | EnableCombine | Boolean | False |  |  |  |
| Unknown | 0ab3a430-7183-4863-b8e3-5b8f4bdee557 | RecipeGetCnt | Int32 | 0 |  |  |  |
| Unknown | 4712e8d7-44a6-4db6-ae23-a93e2dc513e5 | HighlightRecipeList | Boolean | False |  |  |  |
| Likely | 8c3c88a3-2b25-47d9-b782-884842854487 | HighlightCombineIcon | Boolean | False | UI guidance state: highlights highlight combine icon. | Written by `Tutorial_Combine_BurnerSubMenu` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HighlightCombineIcon true (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=HighlightCombineIcon (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 1fe82cd6-9c23-4bee-9cdd-41ed019d1953 | HighlightAlbum | Boolean | False |  |  |  |
| Unknown | ead66deb-74ca-4264-8602-58182e3d97d1 | EnableGenomeCodexRadar | Boolean | False |  |  |  |
| Unknown | 69239436-1540-4f95-966f-f3bd86bfb9be | GenomeCodexRadarNoiseLv | Uint32 | 0 |  |  |  |
| Unknown | 419a0691-1219-4447-b927-e31ac6e35486 | UnlockedDictionaryCombine | Uint32 | 0 |  |  |  |
| Likely | 8787b2c3-044a-4325-b971-58b9592cbe4b | HighlightCraftTab | Boolean | False | UI guidance state: highlights highlight craft tab. | Written by `Tutorial_Craft` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HighlightCraftTab true (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |

### Tutorial_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 346a898f-0151-40c8-a7f2-aa7b1336b540 | Tutorial_Run | Boolean | False |  |  |  |
| Likely | 70f07b7a-89be-484f-960d-330b4c7d0700 | Tutorial_Crouch | Boolean | False | Tutorial prompt state: tracks tutorial state for tutorial crouch. | Written by `Tutorial_Stand_Remind` in `c01_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets Tutorial_Crouch false (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Unknown | dd4323c2-d470-4290-a965-0c0180a7bf23 | Tutorial_CheckInventory | Boolean | False |  |  |  |
| Unknown | 66352132-cea2-448c-a625-3f01be64d671 | Tutorial_Map | Boolean | False |  |  |  |
| Unknown | 2b74efcc-0b1c-4600-b270-ad39f2972dc6 | Tutorial_Attack | Boolean | False |  |  |  |
| Likely | a8cb26cf-5459-4dfc-828b-72841e06816a | Tutorial_Aim_Attack | Boolean | False | Tutorial prompt state: tracks tutorial state for tutorial aim attack. | Written by `c01_Tutorial_Shot` in `c01_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets Tutorial_Aim_Attack false (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | d91d3f36-d349-4476-8c1e-340106f7149a | Tutorial_EquipChange | Boolean | False | Tutorial prompt state: tracks tutorial state for tutorial equip change. | Written by `Tutorial_EqulipChange_Remind` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets Tutorial_EquipChange true (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Tutorial_EquipChange (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 261c7fe9-3f26-467c-90be-87da1b79a815 | Tutorial_WatchFile | Boolean | False |  |  |  |
| Likely | 3847fa5e-7012-4ce8-b0db-aa31168e3740 | Tutorial_Combine | Boolean | False | Tutorial prompt state: tracks tutorial state for tutorial combine. | Written by `Tutorial_Craft` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets Tutorial_Combine false (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Tutorial_Combine (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 748c6b2b-a627-4977-bd9a-4a2e0d13145d | Tutorial_WatchCombineList | Boolean | False |  |  |  |
| Unknown | 9d8b067e-b561-4aca-969c-b33914f95c22 | Tutorial_ItemBox | Boolean | False |  |  |  |
| Unknown | 04f32523-5818-4919-9bdc-b96108a7dd6d | Tutorial_Glasses_Clancy | Boolean | False |  |  |  |
| Unknown | 2f102da5-27c7-4c7c-b6e1-8a553f720351 | Tutorial_Glasses_Ethan | Boolean | False |  |  |  |
| Unknown | bf96bc5f-5468-4d8d-bbbb-260448228ed9 | Tutorial_Tattoo_Clancy | Boolean | False |  |  |  |
| Unknown | d2cab525-2dc8-4bbe-9d1d-8a1ba2f830f9 | Tutorial_Tattoo_Ethan | Boolean | False |  |  |  |
| Unknown | 6ae0bfeb-dc7d-4cee-9f2d-03b1632a3dda | Tutorial_Tracker | Boolean | False |  |  |  |
| Likely | b4dac676-3a6c-4d47-8076-82f49ecc07c9 | Tutorial_QuickSlot | Boolean | False | Tutorial prompt state: tracks tutorial state for tutorial quick slot. | Read by `Tutorial_EqulipChange` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Tutorial_QuickSlot (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | d496d317-4789-4601-a169-859a982c8492 | Tutorial_OpenDoor | Boolean | False |  |  |  |
| Unknown | 4ea42a3d-a505-48aa-9fd8-564ee9f549d9 | Tutorial_QuickTurn | Boolean | False |  |  |  |
| Unknown | f631c13b-89ea-4006-bfcc-7ff07a429dbb | Tutorial_Mind | Boolean | False |  |  |  |
| Unknown | 773ab49f-6b05-4312-b073-50b5a738c5e3 | Tutorial_CloseDoor | Boolean | False |  |  |  |
| Unknown | 49d5dc79-ca0b-451e-8fa3-8c0e79b10187 | Tutorial_Combine_Extra | Boolean | False |  |  |  |

### c03_Objective_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 6bb82bc3-4498-4ce1-818a-2baf248157c9 | c03_objective_FloorDoorLock_Interacted | Boolean | False | Objective tracker: tracks the locked-door interaction for floor door lock interacted. | Written by `100_c03_objective_FloorDoorLock_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_FloorDoorLock_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_FloorDoorLock_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 1aa2078a-17ee-4071-8336-64a2e2d239ce | c03_objective_FloorDoor_Open | Boolean | False | Objective tracker: marks that floor door open is open. | Written by `300_c03_objective_FloorDoor_Open` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_FloorDoor_Open true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_FloorDoor_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 609d15b0-d801-4044-a1c1-17bb9135e306 | c03_objective_TelephoneLaundryRoom_TalkEnd | Boolean | False | Objective tracker: marks that telephone laundry room talk has ended/cleared. | Written by `500_c03_Objective_TelephoneLaundryRoom_TalkEnd` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_TelephoneLaundryRoom_TalkEnd true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_TelephoneLaundryRoom_TalkEnd (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | e08b5d7f-0cd2-40ab-aa87-48ce352a04c5 | c03_objective_OnePlate_Get | Boolean | False | Objective tracker: marks that the player obtained one plate. | Written by `0_c03_objective_OnePlate_Get` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_OnePlate_Get true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_OnePlate_Get (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 410d8f22-7c08-40e2-876b-85a782abf803 | c03_objective_PlateDoor_Approaching | Boolean | False | Objective tracker: tracks plate door approaching. | Written by `0_c03_objective_PlateDoor_Approaching` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_PlateDoor_Approaching true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PlateDoor_Approaching (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 1103536c-596a-420a-bb41-0d83213942a9 | c03_objective_AccessPlateDoor_AND | Boolean | False | Objective tracker: tracks access plate door and. | Written by `0_c03_objective_AccessPlateDoor` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_AccessPlateDoor_AND true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PlateGathering_AND (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 95786014-fd58-45f0-8cb1-c7982fbb4c0d | c03_objective_ByeByeDave_EventEnd | Boolean | False | Objective tracker: marks that bye bye dave event has ended/cleared. | Written by `900_c03_objective_ByeByeDave_EventEnd` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_ByeByeDave_EventEnd true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_ByeByeDave_EventEnd (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | f6284276-8322-42c3-b42e-4a2fc1c44dde | c03_objective_PlayRoomMemo_Interacted | Boolean | False | Objective tracker: marks that the player interacted with play room memo. | Written by `0_c03_objective_PlayRoomMemo_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_PlayRoomMemo_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PlayRoomMemo_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | d39a8bf0-6683-4ad8-bd9e-323a64efd0c3 | c03_objective_SafeRoomMemo_3CrestHint_Interacted_Or | Boolean | False | Objective tracker: marks that the player interacted with safe room memo 3 crest hint interacted or. | Written by `400_c03_objective_SafeRoomMemo_3CrestHint_Interacted_Or` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_SafeRoomMemo_3CrestHint_Interacted_Or true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_SafeRoomMemo_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | ca763662-27c5-4626-8aa8-a4b4417d3411 | c03_objective_SoukoPlate_Interacted | Boolean | False | Objective tracker: marks that the player interacted with storage plate. | Written by `500_c03_objective_SoukoPlate_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_SoukoPlate_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_SoukoPlate_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 94c7dea7-45d2-4a16-a1d5-a40328fe6513 | c03_objective_ReliefInTaxidermyRoom_Get | Boolean | False | Objective tracker: marks that the player obtained relief in taxidermy room. | Written by `0_c03_objective_ReliefInTaxidermyRoom_Get` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_ReliefInTaxidermyRoom_Get true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_ReliefInTaxidermyRoom_Get (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 4f0139a3-6c6f-4633-b3e0-a02f230a4e92 | c03_objective_WatchSoukoPlateState | Boolean | False | Objective tracker: tracks watch storage plate state. | Written by `500_c03_objective_WatchSoukoPlateState` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_WatchSoukoPlateState true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_WatchSoukoPlateState (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 51c31d9d-0a91-4f33-8770-30d9c708e1d9 | c03_objective_3ReliefHint_Interacted | Boolean | False | Objective tracker: marks that the player interacted with 3 relief hint. | Written by `400_c03_objective_3ReliefHint_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_3ReliefHint_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_3ReliefHint_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 47f82bc0-b598-46f4-9260-4a1ed508fea9 | c03_objective_FullFillReliefGetCondition_AND | Boolean | False | Objective tracker: toggles full fill relief get condition and. | Written by `400_c03_objective_FullFillReliefGetCondition_AND` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_FullFillReliefGetCondition_AND true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_FullFillReliefGetCondition_AND (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 53c0f7ba-5800-4f13-ae08-6dec96da348b | c03_objective_ShotGunStatue_Approaching | Boolean | False | Objective tracker: tracks shot gun statue approaching. | Written by `0_c03_Objective_ShotGunStatue_Approaching` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_ShotGunStatue_Approaching true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_ShotGunStatue_Approaching (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 3c822de9-75c5-4851-ac7f-ff7ac37eaa50 | c03_objective_SolvePuzzle | Boolean | False | Objective tracker: tracks puzzle progress for solve puzzle. | Written by `400_c03_objective_SolvePuzzle` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_SolvePuzzle true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_SolvePuzzle (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | ff4f2de4-24a3-4cc3-af92-c068e68c0ac0 | c03_objective_2JackBattle_AreaOut | Boolean | False | Objective tracker: marks that the player left 2 Jack battle area out. | Written by `700_c03_objective_2JackBattle_AreaOut` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_2JackBattle_AreaOut true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_2JackBattle_AreaOut (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | f7dff8d0-10a0-461e-b5bf-2eac48a0f513 | c03_objective_HASAMIJackAreaDoorLock_Interacted | Boolean | False | Objective tracker: tracks the locked-door interaction for hasamijack area door lock interacted. | Written by `500_c03_objective_HASAMIJackAreaDoorLock_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_HASAMIJackAreaDoorLock_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_HASAMIJackAreaDoorLock_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 92273ade-1ba0-4f5d-9ed8-fb7ea84c6fb5 | c03_objective_HASAMIJackAreaDoorLockCantOpen | Boolean | False | Objective tracker: tracks the locked-door interaction for hasamijack area door lock cant open. | Written by `700_c03_objective_HASAMIJackAreaDoorLockCantOpen` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_HASAMIJackAreaDoorLockCantOpen true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_HASAMIJackAreaDoorLockCantOpen (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | c0d8c707-aefb-49fc-b997-c2341bd9e7f8 | c03_objective_LivingRoomRelief_Get | Boolean | False | Objective tracker: marks that the player obtained living room relief. | Written by `0_c03_objective_LivingRoomRelief_Get` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_LivingRoomRelief_Get true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_LivingRoomRelief_Get (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | f462332e-fb5a-456a-8409-57bf662bc3f3 | c03_Objective_SetShotGunToStatue | Boolean | False | Objective tracker: tracks set shot gun to statue. | Written by `400_c03_objective_SetShotGunToStatue` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_Objective_SetShotGunToStatue true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_Objective_SetShotGunToStatue (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | e7cee1f3-dd88-4274-8fc5-7945bd693451 | c03_objective_PlayRoomRelief_Get | Boolean | False | Objective tracker: marks that the player obtained play room relief. | Written by `0_c03_objective_PlayRoomRelief_Get` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_PlayRoomRelief_Get true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PlayRoomRelief_Get (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Unknown | 69b40dbd-189b-48d4-86ea-c1ce50a12d2d | c03_objective_TelephoneTrailerHouse_Ring | Boolean | False |  |  |  |
| Likely | de3d2dc4-e389-431f-86a8-bf0ca11954d8 | c03_objective_MotherMemo_Interacted | Boolean | False | Objective tracker: marks that the player interacted with Marguerite memo. | Written by `300_c03_objective_MotherMemo_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_MotherMemo_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_MotherMemo_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | e6bcc25f-5172-401d-a9ee-e8e548f58b25 | c03_objective_Hyouhon_Interacted | Boolean | False | Objective tracker: marks that the player interacted with specimen. | Written by `700_c03_objective_Hyouhon_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_Hyouhon_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_Hyouhon_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | b26e6ddd-3b72-490a-9202-d3f64cd615aa | c03_objective_SaidanMemo_Interacted | Boolean | False | Objective tracker: marks that the player interacted with altar memo. | Written by `700_c03_objective_SaidanMemo_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_SaidanMemo_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_SaidanMemo_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | d35400e7-fcef-4be1-b8fe-a8d2c229f9f8 | c03_objective_LanternDoor_Approaching | Boolean | False | Objective tracker: tracks lantern door approaching. | Written by `700_c03_objective_LanternDoor_Approaching` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_LanternDoor_Approaching true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_LanternDoor_Approaching (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | bcffd04e-f378-456b-a2bd-12ee9fe91397 | c03_objective_WatchTenagaHahaEscaped_InsectHole | Boolean | False | Objective tracker: tracks watch long-arm Marguerite escaped insect hole. | Written by `900_c03_objective_WatchTenagaHagaEscaped_InsectHole` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_WatchTenagaHahaEscaped_InsectHole true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_WatchTenagaHahaEscaped_InsectHole (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 92bbf6b3-6219-46f7-932c-d3deb01a2b7f | c03_objective_LanternGet | Boolean | False | Objective tracker: marks that the player obtained lantern. | Written by `1200_c03_objective_LanternGet` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_LanternGet true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_LanternGet (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Unknown | 0acf2e67-3acf-4509-8c5f-96ca72ceb70d | c03_objective_HangUpLucasPhone | Boolean | False |  |  |  |
| Likely | 2f09d3c8-b78f-4ad6-8f3d-bc56bd4ae520 | c03_objective_StopMeetZoeCondition_OR | Boolean | False | Objective tracker: toggles stop meet Zoe condition or. | Written by `25_c03_objective_StopMeetZoeCondition` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_StopMeetZoeCondition_OR true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_StopMeetZoeCondition_OR (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 12b302a1-9093-4518-9ba0-6e3cb2ef6caa | c03_objective_DaveNeckMemo_Interacted | Boolean | False | Objective tracker: marks that the player interacted with dave neck memo. | Written by `25_c03_objective_DaveNeckMemo_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_DaveNeckMemo_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_DaveNeckMemo_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 20dc58ff-00cc-4541-a149-90fbf6e619b2 | c03_objective_IntoBedRoomMainHouse2F | Boolean | False | Objective tracker: marks that the player entered bed room main house 2F. | Written by `50_c03_objective_IntoBedRoomMainHouse2F` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_IntoBedRoomMainHouse2F true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_IntoBedRoomMainHouse2F (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | c8f795d3-f0f1-4dd4-b006-e5ba4f360d95 | c03_objective_IntoKidsRoomMainHouse2F | Boolean | False | Objective tracker: marks that the player entered kids room main house 2F. | Written by `50_c03_objective_IntoKidsRoomMainHouse2F` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_IntoKidsRoomMainHouse2F true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_IntoKidsRoomMainHouse2F (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 58e1adcb-e305-4db4-823a-86cb35f8da05 | c03_Objective_2LucasCardKey_Get | Boolean | False | Objective tracker: marks that the player obtained 2 Lucas card key. | Written by `100_c03_Objective_2LucasCardKey_Get` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_Objective_2LucasCardKey_Get true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_Objective_2LucasCardKey_Get (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | cab62fff-3a5e-4bf1-8549-baf28e4ad6a3 | c03_objective_LucasMonitorRoom_AreaIn | Boolean | False | Objective tracker: marks that the player entered Lucas monitor room area in. | Written by `100_c03_objective_LucasMonitorRoom_AreaIn` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_LucasMonitorRoom_AreaIn true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_LucasMonitorRoom_AreaIn (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Unknown | 7ec835e1-a32b-48eb-9a6f-88679c5d2166 | c03_objective_BackyardMonitor_EventEnd | Boolean | False |  |  |  |
| Unknown | f13ec50f-b299-4e78-99ba-523fcfd4527c | c03_objective_ParentsRoomRiddle_Interacted | Boolean | False |  |  |  |
| Likely | 2cfdb742-0b66-4ffa-959b-2ff1397dc6fd | c03_objective_ClancyDeadBodyMemo_Interacted | Boolean | False | Objective tracker: marks that the player interacted with clancy dead body memo. | Written by `900_c03_objective_ClancyDeadBodyMemo_Interacted` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_ClancyDeadBodyMemo_Interacted true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_ClancyDeadBodyMemo_Interacted (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | f80d2770-d13b-400e-a749-5261a1366023 | c03_objective_CorrectPasscordSet | Boolean | False | Objective tracker: tracks correct passcord set. | Written by `1000_c03_objective_CorrectPasscordSet` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_CorrectPasscordSet true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_CorrectPasscordSet (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 36107bb9-d80f-497a-8746-597900cbfbea | c03_objective_PazzleRoomNoItem_AreaIn | Boolean | False | Objective tracker: marks that the player entered puzzle room no item area in. | Written by `1000_c03_objective_PazzleRoomNoItem_AreaIn` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_PazzleRoomNoItem_AreaIn true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PazzleRoomNoItem_AreaIn (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | f804ad1e-969b-43fa-bc5f-15715f61917d | c03_Objective_EnterPuzzleRoomCondition_OR | Boolean | False | Objective tracker: marks that the player entered puzzle room condition or. | Written by `900_c03_objective_EnterPuzzleRoomCondition_OR` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_Objective_EnterPuzzleRoomCondition_OR true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_Objective_EnterPuzzleRoomCondition_OR (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | dc48ed79-b768-430b-b5b5-8f4a2973af07 | c03_objective_PazzleRoomLucasVoice_MessageEnd | Boolean | False | Objective tracker: marks that puzzle room Lucas voice message has ended/cleared. | Written by `1040_c03_objective_PazzleRoomLucasVoice_MessageEnd` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_PazzleRoomLucasVoice_MessageEnd true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PazzleRoomLucasVoice_MessageEnd (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 6ab85a41-cd57-461c-805a-ef0f9d449676 | c03_objective_PazzleRoom_AreaOut | Boolean | False | Objective tracker: marks that the player left puzzle room area out. | Written by `1280_c03_objective_PazzleRoom_AreaOut` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_PazzleRoom_AreaOut true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_PazzleRoom_AreaOut (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 21411c8c-2b95-418e-8efa-8bf79bae4ae5 | c03_objective_EvlineFace_Get | Boolean | False | Objective tracker: marks that the player obtained Eveline face. | Written by `1300_c03_objective_EvlineFace_Get` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_EvlineFace_Get true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_EvlineFace_Get (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 9706c3d9-93a5-4278-aa4b-516f5b625834 | c03_objective_MonitorEventEndInBackYard | Boolean | False | Objective tracker: marks that monitor event end in back yard has ended/cleared. | Written by `1300_c03_objective_MonitorEventEndInBackYard` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_MonitorEventEndInBackYard true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_MonitorEventEndInBackYard (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 312ffa35-276e-487c-a7b0-12c10fcffae1 | c03_objective_CompleteSeekingBackyard_AND | Boolean | False | Objective tracker: tracks complete seeking backyard and. | Written by `1300_c03_objective_CompleteSeekingBackyard_AND` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_objective_CompleteSeekingBackyard_AND true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_objective_CompleteSeekingBackyard_AND (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 193e35f9-2b66-491c-9fd9-f3dff61943b5 | c03_Objective_MeetZoeNot2SerumMaterial | Boolean | False | Objective tracker: tracks meet Zoe not 2 serum material. | Written by `200_c03_Objective_MeetZoeNot2SerumMaterial` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_Objective_MeetZoeNot2SerumMaterial true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_Objective_MeetZoeNot2SerumMaterial (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 75e18a03-28ab-4d51-bdc3-2f7198f3df79 | c03_Objective_Deliver2SerumMaterials | Boolean | False | Objective tracker: tracks deliver 2 serum materials. | Written by `200_c03_Objective_Deliver2SerumMaterials` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_Objective_Deliver2SerumMaterials true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_Objective_Deliver2SerumMaterials (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |
| Likely | 0c714701-7e0f-4736-aa29-2d2f53593f2a | c03_Objective_WatchZoeAtPier | Boolean | False | Objective tracker: tracks watch Zoe at pier. | Written by `900_c03_Objective_WatchZoeAtPier` in `objectivefsm_c03.scn.20`. | Write via `via.fsm.action.SetBool`: sets c03_Objective_WatchZoeAtPier true (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c03_Objective_WatchZoeAtPier (`leveldesign/fsm/chapter3/objectivefsm_c03.scn.20`) |

### TutorialCheck_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | e445b2f5-a472-4f45-8505-f5dfaef97823 | HadRun | Boolean | False | Tutorial tracker: records that the player run. | Written by `Tutorial_Hadrun` in `c01_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HadRun true (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=HadRun (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | e440edb3-68b0-4296-a861-959dc9ca89a6 | HadCamera | Boolean | False | Tutorial tracker: records that the player camera. | Read by `Tutorial_Camera` in `c01_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=HadCamera (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | 73950c79-d514-4891-bec8-28f2137d5b8b | HadUsedRemedy | Boolean | False | Tutorial tracker: records that the player used remedy. | Written by `Tutorial_CutLeg` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HadUsedRemedy false (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=HadUsedRemedy (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | ab2f4576-0b21-4d25-aa25-4192d8c61d78 | HadUsedStimulant | Boolean | False | Tutorial tracker: records that the player used stimulant. | Written by `Tutorial_Use_Stimulant` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HadUsedStimulant false (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=HadUsedStimulant (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Likely | 7f7f9452-24a3-4345-999a-9206a8ca4ee1 | HadUsedDepressant | Boolean | False | Tutorial tracker: records that the player used depressant. | Written by `Tutorial_Use_Depressant` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HadUsedDepressant false (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=HadUsedDepressant (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 3db817cb-fa8d-4a44-8124-ef154668e76e | HadCrouch | Boolean | False |  |  |  |
| Unknown | 59844303-2d0c-4ceb-9eb6-46c8964d7fa9 | HadStandUp | Boolean | False |  |  |  |
| Unknown | 522905f7-66d6-42b2-8f47-52252de86843 | Check_OpenedDoor | Boolean | False |  |  |  |
| Likely | 9a09610a-0561-42e7-9706-c5ab7bf6c1ab | HadReloaded | Boolean | False | Tutorial tracker: records that the player reloaded. | Written by `c01_Tutorial_Reload` in `c01_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HadReloaded true (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=HadReloaded (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | 1efad4cd-4cd8-4836-b683-23ce23602ce3 | OpenInventory | Boolean | False | Tutorial tracker: tracks opening open inventory. | Read by `Tutorial_Craft` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=OpenInventory (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Likely | 7b7952e5-ccc2-47eb-a711-6ef785590151 | OpenTutorial | Boolean | False | Tutorial tracker: tracks opening open tutorial. | Read by `Tutorial_AttackHandAxe` in `c01_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=OpenTutorial (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | f8132142-0ecc-45a5-8fa8-57176f3beec4 | HadWalked | Boolean | False | Tutorial tracker: records that the player walked. | Read by `Tutorial_Walk` in `c01_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=HadWalked (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Unknown | 4df50d74-1c12-493b-a0e0-cb1ef4f50f41 | OpenMap | Boolean | False |  |  |  |
| Likely | f4d99abc-1a2c-43ff-b17d-996c7588fea7 | GotBurner | Boolean | False | Tutorial tracker: records that the player obtained burner. | Read by `Tutorial_CK_GotBurner` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=GotBurner (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Likely | 0fa1edc2-52c1-4ada-bda7-22619815d94d | InventoryTabCraft | Boolean | False | Tutorial tracker: tracks inventory/UI state for inventory tab craft. | Read by `Tutorial_Craft` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=InventoryTabCraft (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 7abc0fc6-aec6-4e11-b1fb-5a25cb825a97 | HadMoved | Boolean | False |  |  |  |
| Likely | 6bb2d6e8-0e64-43cf-b69e-68825f9f4436 | OpenDetailSearch | Boolean | False | Tutorial tracker: tracks opening open detail search. | Read by `Tutorial_DetailSearch_MiaHandBag` in `c01_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=OpenDetailSearch (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | 3e17affc-f3c0-428d-ba18-cb90e8b53069 | SawStandTutorial | Boolean | False | Tutorial tracker: tracks tutorial state for saw stand tutorial. | Written by `Tutorial_Stand` in `c01_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets SawStandTutorial true (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=SawStandTutorial (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | ebc38a0e-edcb-4737-94b6-d35094893865 | UsingEyeDrops | Boolean | False | Tutorial tracker: tracks using eye drops. | Read by `Tutorial_EyeDrop` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=UsingEyeDrops (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 527cdc4e-fe23-4d49-8da9-b2c1899d3475 | HasCraftMaterial | Boolean | False |  |  |  |
| Unknown | d314c86e-bbcf-40b1-aedd-9497b3b147d2 | CanOpenMap | Boolean | False |  |  |  |
| Likely | d2fc12fe-0908-43f2-825f-439953762356 | CanOpenInventory | Boolean | False | Tutorial tracker: allows can open inventory. | Read by `Tutorial_Combine_Burner` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=CanOpenInventory (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Unknown | 6c307196-a372-4012-9492-9d2675b0887a | CanChangeWeapon | Boolean | False |  |  |  |
| Likely | 4d51675c-db53-4a45-955d-183acfde0a79 | ChangedWeapon | Boolean | False | Tutorial tracker: toggles changed weapon. | Read by `Tutorial_EqulipChange_Remind` in `c03_tutorial.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=ChangedWeapon (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Likely | 06139dc1-f2d2-44b7-8407-bd6f78b6607e | HadChangedBullet | Boolean | False | Tutorial tracker: records that the player changed bullet. | Written by `Tutorial_ChangeBullet` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets HadChangedBullet true (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=HadChangedBullet (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |
| Likely | 440b536c-c4b4-4061-a271-e77fb18e3610 | UsedRemedy | Boolean | False | Tutorial tracker: records that the player used remedy. | Written by `c03_Tutorial_RemindRecovery` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets UsedRemedy true (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=UsedRemedy (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Likely | 3d7fd452-de91-43f8-9604-16b2ac48bbf9 | ChangedEqulipWeapon | Boolean | False | Tutorial tracker: toggles changed equip weapon. | Written by `Tutorial_EqulipChange_Remind` in `c03_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets ChangedEqulipWeapon true (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=ChangedEqulipWeapon (`leveldesign/fsm/chapter3/c03_tutorial.scn.20`) |

### FF050_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | f1f65a3d-dc4c-4d77-b421-0e65499b91db | FlashBackEnd | Boolean | False | Wrecked ship old videotape flow: marks that flash back has ended/cleared. | Written by `0_Wait_FlashBack` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets FlashBackEnd true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_50.user.2` |
| Likely | 7f2560d7-0803-4232-bad8-fe97a7e717a6 | StartEventEnd | Boolean | False | Wrecked ship old videotape flow: marks that start event has ended/cleared. | Written by `0_StartEvent_End` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets StartEventEnd true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=StartEventEnd (`leveldesign/fsm/ff050/objectivefsm_ff050.scn.20`) |
| Likely | 7b83f041-b894-4d4f-b4e6-97df81f8d07c | Check_Trace_1 | Boolean | False | Wrecked ship old videotape flow: tracks check trace 1. | Written by `100_CK_Trace_01` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Check_Trace_1 true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_200.user.2` |
| Likely | aa3f4003-7373-4d01-b3bd-6caaf9ad91fd | Check_Trace_ï¼’ | Boolean | False | Wrecked ship old videotape flow: tracks check trace. | Written by `200_Em3500_InElevator` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Check_Trace_２ true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_300.user.2` |
| Likely | 014c1de5-2d67-48d9-bf41-c131ee2d078c | Reaching_B2F | Boolean | False | Wrecked ship old videotape flow: tracks reaching b 2F. | Written by `300_Reaching_B2F` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Reaching_B2F true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_400.user.2` |
| Likely | d132a863-8606-447a-a3ce-a6e4aa70daeb | Interact_Specimen | Boolean | False | Wrecked ship old videotape flow: marks that the player interacted with specimen. | Written by `400_InteractTest` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Interact_Specimen true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_450.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=Interact_Specimen (`leveldesign/fsm/ff050/objectivefsm_ff050.scn.20`) |
| Likely | 80611f89-2073-4bc9-8df6-0d549254181e | StartVoiceEvent | Boolean | False | Wrecked ship old videotape flow: tracks start voice event. | Written by `450_Start_Event_trg` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets StartVoiceEvent true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_475.user.2` |
| Likely | b8ba6d11-dc3f-4243-8f74-577406c7af41 | EndVoiceEvent | Boolean | False | Wrecked ship old videotape flow: tracks end voice event. | Written by `475_Em3500_Runaway` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets EndVoiceEvent true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_500.user.2` |
| Likely | e852abe4-d3cf-4c02-b992-335ee490315e | Ev_Push_3F | Boolean | False | Wrecked ship old videotape flow: tracks ev push 3F. | Written by `500_Interact_3FButton` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Ev_Push_3F true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_600.user.2` |
| Likely | 27368c49-c794-45bb-81fb-c1d9489310af | Kick_Buddy_Event | Boolean | False | Wrecked ship old videotape flow: tracks kick buddy event. | Written by `600_Start_BuddyEvent_Trg` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Kick_Buddy_Event true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_700.user.2` |
| Likely | 1c894a9c-8a53-4391-aba6-23e2334f917b | End_Buddy_Event | Boolean | False | Wrecked ship old videotape flow: tracks end buddy event. | Written by `700_BuddyEvent_End` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets End_Buddy_Event true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_800.user.2` |
| Likely | c56e15d8-35fc-41d9-9630-86d6c4d43974 | Kick_Last_Event | Boolean | False | Wrecked ship old videotape flow: tracks kick last event. | Written by `800_KickLastEvent_Start` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets Kick_Last_Event true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_900.user.2` |
| Likely | 941c1fb9-3220-4f9c-aefc-073409e15e6d | End_Last_Event | Boolean | False | Wrecked ship old videotape flow: tracks end last event. | Written by `800_LastEvent_End` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets End_Last_Event true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Gate package `ff050_main_end.user.2` |

### FF050_Flag

_No variables were listed under this group in the captured baseline log._

### c01_Objective_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | c9a1bb7f-db55-48bb-a899-79704d1f9036 | c01_objective_SecondDoorOpen_Interacted | Boolean | False | Objective tracker: marks that second door open interacted is open. | Written by `100_objective_SecondDoorOpen_Interact` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_SecondDoorOpen_Interacted true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_SecondDoorOpen_Interacted (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | f11426ab-6173-4160-9486-e1148b90cf7d | c01_objective_VideoTapeGet_Interact | Boolean | False | Objective tracker: marks that the player interacted with video tape. | Written by `100_objective_VideoTapeGet_ItemUse` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_VideoTapeGet_Interact true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_VideoTapeGet_Interact (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | e20d809e-f164-4a2a-8a64-17459b056bba | c01_objective_StoveSwitchOpenArea_AreaIn | Boolean | False | Objective tracker: marks that the player entered stove switch open area area in. | Written by `100_objective_SecretDoorPassing_AreaIn` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_StoveSwitchOpenArea_AreaIn true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_StoveSwitchOpenArea_AreaIn (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 604fe9fe-b9ba-496d-92fd-5f45359d164f | c01_objective_MiaDiscover_Interacted | Boolean | False | Objective tracker: marks that the player interacted with Mia discover. | Written by `200_objective_MiaDiscover_Interacted` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_MiaDiscover_Interacted true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_MiaDiscover_Interacted (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | f063d396-0835-497b-8b29-7adb1b9194f3 | c01_objective_ChainCut_ItemUse | Boolean | False | Objective tracker: marks item use for chain cut item use. | Written by `200_objective_ChainCut_ItemUse` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_ChainCut_ItemUse true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_ChainCut_ItemUse (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 0a80c571-7d1d-4d2a-a3c4-7689ab36fd8c | c01_objective_KidnapMia_AreaIn | Boolean | False | Objective tracker: marks that the player entered kidnap Mia area in. | Written by `500_objective_KidnapMia_AreaIn` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_KidnapMia_AreaIn true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_KidnapMia_AreaIn (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 448a0802-065d-4479-ab99-d0496d68ddbf | c01_objective_OutgoingSafeRoom_AreaIn | Boolean | False | Objective tracker: marks that the player entered outgoing safe room area in. | Written by `500_objective_OutgoingSafeRoom_AreaIn` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_OutgoingSafeRoom_AreaIn true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | a74aa0b9-915f-44e4-9440-d8a39cd74f51 | c01_objective_EnemyMiaAppear_EventEnd | Boolean | False | Objective tracker: marks that enemy Mia appear event has ended/cleared. | Written by `600_objective_EnemyMiaAppear_EventEnd` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_EnemyMiaAppear_EventEnd true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_EnemyMiaAppear_EventEnd (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 1b903f88-087e-4b65-94db-ca35f254f127 | c01_objective_EnemyMiaGekichin_BattleEnd | Boolean | False | Objective tracker: marks that enemy Mia gekichin battle end battle is over. | Written by `600_FirstBattleEnd` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_EnemyMiaGekichin_BattleEnd true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_EnemyMiaGekichin_BattleEnd (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 0426aa22-a836-40bf-9149-05cd1e249e95 | c01_objective_EnemyMiaSecondBattle_EventStart | Boolean | False | Objective tracker: marks that enemy Mia second battle event has started. | Written by `800_objective_SecondBattleStart_BattleSet` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_EnemyMiaSecondBattle_EventEnd true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_EnemyMiaSecondBattle_EventEnd (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 1ec5020a-35b5-4f8b-ba56-b78dc12baaee | c01_objective_ZoyTelephone_TalkEnd | Boolean | False | Objective tracker: marks that Zoe telephone talk has ended/cleared. | Written by `1000_objective_ZoyTelephone_TalkEnd` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_ZoyTelephone_TalkEnd true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_ZoyTelephone_TalkEnd (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | dcabd236-8ee3-42c2-a0f1-5918eca53695 | c01_objective_StairsSwitch_Interacted | Boolean | False | Objective tracker: marks that the player interacted with stairs switch. | Written by `1000_objective_StairsSwitch_Interacted` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_StairsSwitch_Interacted true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_StairsSwitch_Interacted (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | f44bd049-4889-4da9-a9cd-6424414fc47e | c01_objective_StairsSwitch_CheckFSM | Boolean | False | Objective tracker: tracks stairs switch check fsm. | Written by `1050_objective_StairsSwitch_CheckFSM` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_StairsSwitch_CheckFSM true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_StairsSwitch_CheckFSM (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Unknown | ec590aee-13fe-46a0-a44e-277fc6e96554 | c01_objective_MiaBattleAllEnd | Boolean | False |  |  |  |
| Likely | 330d1bfe-47fb-409d-b933-7393ab66132f | c01_objective_KnockDoorSoundStart | Boolean | False | Objective tracker: marks that knock door sound has started. | Written by `500_objective_KnockDoorSoundStart` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_KnockDoorSoundStart true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_KnockDoorSoundStart (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Likely | 71447ce8-3ad0-48d5-b439-c27e81911ff0 | c01_objective_NearBuilding_AreaIn | Boolean | False | Objective tracker: marks that the player entered near building area in. | Written by `50_objective_NearBuilding_AreaIn` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_NearBuilding_AreaIn true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_NearBuilding_AreaIn (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |
| Unknown | 62f04b8e-9060-4c88-92d3-c58b2acc5f2e | c01_objective_StoveSwitch_Interacted | Boolean | False |  |  |  |
| Likely | 2ef59fc8-44fa-4eec-a6dd-81b378d6798c | c01_objective_ZoyTelephone_CallLead | Boolean | False | Objective tracker: tracks the call/message state for Zoe telephone call lead. | Written by `900_objective_ZoyTelephone_CallLead` in `objectivefsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets c01_objective_ZoyTelephone_CallLead true (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c01_objective_ZoyTelephone_CallLead (`leveldesign/fsm/chapter1/objectivefsm_c01.scn.20`) |

### Battle_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 2a59fc71-a137-406d-a0f3-ecdddead93e7 | Battle_SaidAimGunForGrapple | Boolean | False |  |  |  |
| Unknown | f73ae845-cb1e-4698-907d-ac5ea33d982f | Battle_SaidAimKnifeForGrapple | Boolean | False |  |  |  |
| Unknown | 17c31b9b-a8a0-4626-9625-8b76c34f55df | Battle_SaidAimBombForGrapple | Boolean | False |  |  |  |
| Unknown | 85cbba7d-17fb-48b0-9850-25baea4b6c8a | Battle_SaidAimGunForGrappleChapter1 | Boolean | False |  |  |  |
| Unknown | 4ab87047-4beb-4d2f-86f4-911e718e024f | Battle_SaidAimKnifeForGrappleChapter1 | Boolean | False |  |  |  |
| Unknown | e873d3e1-0d9e-48f0-985a-cb3c1638076e | Battle_CounterableForGrapple | Int32 | 0 |  |  |  |
| Unknown | 4475d01b-2c12-4c03-abc8-3e382df91775 | Battle_ResistableForGrapple | Int32 | 0 |  |  |  |
| Unknown | ce17ccef-19a4-4040-893a-fbe12ed19d35 | Battle_CounterableBombForGrapple | Int32 | 0 |  |  |  |
| Unknown | 0c8336fd-ff8b-427e-af0a-e515216c45db | Battle_IsStaySafeZone | Boolean | False |  |  |  |

### AchievementStats

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 6da386a3-6bd1-4094-8314-6d8970ecbee9 | Achievement_CountOfKilledByKnife | Int32 | 0 |  |  |  |
| Unknown | 9db89a4d-2e6e-4719-b16c-479be8b40280 | Achievement_CountOfKilledByAttachBomb | Int32 | 0 |  |  |  |
| Unknown | 37c57840-5756-4b87-811c-f4804803a49d | Achievement_CountOfUsedCure | Int32 | 0 |  |  |  |
| Unknown | e2aa6c39-bf8a-4dd2-a88f-795f5c4a7670 | Achievement_CountOfOpenItemBox | Int32 | 0 |  |  |  |
| Unknown | fe3fd5b7-5dab-425b-b259-bad236edfaa6 | Achievement_CountOfRepulsedInTheAir | Int32 | 0 |  |  |  |
| Unknown | a1cc3ded-7c40-45c3-9418-ed3bf926585e | Achievement_CountOfRepulsedMother | Int32 | 0 |  |  |  |
| Unknown | 3cef790d-fe45-48ab-95e7-bb8419bcba21 | Achievement_CountOfCloseDoors | Int32 | 0 |  |  |  |
| Unknown | 3c0900cd-80d8-4f2b-8371-ca8761c4fd02 | Achievement_CountOfBrokenInsectDoorsByKnife | Int32 | 0 |  |  |  |
| Unknown | 1c087a05-cb61-4138-9d2d-7c5dd0c81bdf | Achievement_CountOfSucceededGuards | Int32 | 0 |  |  |  |
| Unknown | 950deecf-1213-4576-8d93-f40a4d418a68 | Achievement_CountOfBrokenAllMoldedParts | Int32 | 0 |  |  |  |
| Unknown | 6f11efa8-6e69-4872-b851-6126722c8b0d | Achievement_CountOfTwoKilledAtOneShot | Int32 | 0 |  |  |  |
| Unknown | c5a91b38-f43e-4c16-999a-de16b2205552 | Achievement_CountOfAvoidBySquat | Int32 | 0 |  |  |  |
| Unknown | 8f3117c2-cccb-4352-94af-b7d8181d11ec | Achievement_CountOfGetItemByDetailSearch | Int32 | 0 |  |  |  |
| Unknown | 2bf526e0-7779-4035-93d2-dcab41860de9 | Achievement_CountOfUsedEyeLotion | Int32 | 0 |  |  |  |
| Unknown | 7f6137ba-9b18-40af-880d-80a4d6cad015 | Achievement_CountOfSetupMissShadowPuzzle | Int32 | 0 |  |  |  |
| Unknown | 2eceb313-3e70-40ad-b133-32e9d2afea92 | Achievement_CountOfFiles | Int32 | 0 |  |  |  |
| Unknown | 0358a957-318e-4d6e-935f-09fc32b52694 | Achievement_CountOfCoins | Int32 | 0 |  |  |  |
| Unknown | 825642a6-f01b-4608-a41f-bf8bf23a05c2 | Achievement_CountOfCoinsForHard | Int32 | 0 |  |  |  |
| Unknown | 5a672cf0-cc12-426e-be0c-d2e86073300b | Achievement_CountOfPicking | Int32 | 0 |  |  |  |
| Unknown | 4bed1fd7-26f8-4b92-87b2-4b63b8d883e1 | Achievement_CountOfStabilizers | Int32 | 0 |  |  |  |
| Unknown | b1a2d58d-77ba-4a32-a84e-66eb177ce7a6 | Achievement_CountOfSteroids | Int32 | 0 |  |  |  |
| Unknown | 5e4de8fb-5a24-4c92-8fd3-26aac400087a | Achievement_CollectOfFormulated | Int32 | 0 |  |  |  |
| Unknown | 2fb4f046-6658-4f81-bbb3-857e22959efe | Achievement_CollectOfClearedFF | Int32 | 0 |  |  |  |
| Unknown | 72164e3a-a5cc-4a65-bea7-fa2ba9bde72b | HeroStats_GetFuse_CHP1 | Boolean | False |  |  |  |
| Unknown | 35ce719c-5e78-4bc5-a4a7-b5be59a83579 | HeroStats_GetFuse_CHP4 | Boolean | False |  |  |  |
| Unknown | adefccce-f366-41e2-ba15-e8ab5b75505f | HeroStats_GetFuse_FF | Boolean | False |  |  |  |
| Unknown | 041212d2-edfd-47b9-8085-66776b0935a9 | HeroStats_GetShotgun_Dummy | Boolean | False |  |  |  |
| Unknown | 3e58b47e-4b19-4cfc-9b2f-50dfa0256853 | HeroStats_GetShotgun_Wp1039 | Boolean | False |  |  |  |
| Unknown | e3c63398-f2c9-4e81-a6de-732d208f92f1 | HeroStats_GetShotgun_Wp1030 | Boolean | False |  |  |  |
| Unknown | 45d2b663-8a3a-4652-b15d-663224d29f77 | HeroStats_GetShotgun_Wp1230 | Boolean | False |  |  |  |
| Unknown | 2f59a250-eba1-4601-a38d-f1ddad1110b9 | HeroStats_GetShotgun_Wp1280 | Boolean | False |  |  |  |
| Unknown | 681f8b38-31e4-4544-b0cb-7af517e65ce9 | HeroStats_GetCandle | Boolean | False |  |  |  |
| Unknown | 878a1395-09e8-4d46-b355-a33c7e647f73 | HeroStats_GetCandleFire | Boolean | False |  |  |  |

### RichPresenceStats

_No variables were listed under this group in the captured baseline log._

### c04_Objective_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | b5ca1472-6747-40d9-9500-73c166e13b0a | c04_objective_EthanDragging_EventEnd | Boolean | False | Objective tracker: marks that Ethan dragging event has ended/cleared. | Written by `25_EthanDragging_EventEnd` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_EthanDragging_EventEnd true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_EthanDragging_EventEnd (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 4c924b46-2346-460b-98c2-3bd73f94e3bc | c04_objective_Hulk_AreaIn | Boolean | False | Objective tracker: marks that the player entered Molded area in. | Written by `25_HulkAreaIn` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_Hulk_AreaIn true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_Hulk_AreaIn (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 9723e391-9676-4011-bb77-fc684cda91aa | c04_objective_FuseBoxInteract | Boolean | False | Objective tracker: marks that the player interacted with fuse box. | Written by `200_FuseInteractSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_FuseBoxInteract true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_FuseBoxInteract (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | f3911a20-4121-4043-b58f-b9750a86d944 | c04_objective_FuseGetInventory | Boolean | False | Objective tracker: tracks inventory/UI state for fuse get inventory. | Written by `200_FuseGetSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_FuseGetInventory true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_FuseGetInventory (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 5105f0c9-20c3-4a85-ac2a-3156934db25a | c04_objective_EvelynSayWatchingVideo | Boolean | False | Objective tracker: tracks Eveline say watching video. | Written by `300_Eveline_VideoTapeYubisashi_SetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_EvelynSayWatchingVideo  true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_EverinSayWatchingVideo  (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 92d0a05f-9b5f-4fa0-9fc7-ac849bb49984 | c04_objective_FF050Start | Boolean | False | Objective tracker: marks that videotape 050 has started. | Written by `300_FF050StartSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_FF050Start true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_FF050Start (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 6e502f94-e399-4acd-b22b-1a66a959a5af | c04_objective_EthanLocalizationPC | Boolean | False | Objective tracker: toggles Ethan localization PC. | Written by `500_EthanLocalzationPC` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_EthanLocalizationPC true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_EthanLocalizationPC (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | a4cd817c-ff01-4479-bd73-8e30113c5d7b | c04_objective_TopBoardInteract | Boolean | False | Objective tracker: marks that the player interacted with top board. | Written by `500_TopBoardInteractSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_TopBoardInteract true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_TopBoardInteract (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 9b34e3e4-a6ce-4d3f-921d-070ac93f8005 | c04_objective_ElevatorGetInsaide | Boolean | False | Objective tracker: tracks elevator get inside. | Written by `600_ElavatorGetInSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorGetInsaide true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorGetInsaide (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 5b8d9e4f-0032-4bc1-84cd-ae2e9ab51468 | c04_objective_ElevatorButtonInteract | Boolean | False | Objective tracker: marks that the player interacted with elevator button. | Written by `600_ElavatorGetOutSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorButtonInteract true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorbuttonInteract (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | ccf86b3d-62ec-4852-a727-b3f4a35eb0b3 | c04_objective_ElevatorFuseBoxInteract | Boolean | False | Objective tracker: marks that the player interacted with elevator fuse box. | Written by `600_ElevatorFuseBoxInteractSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorFuseBoxInteract true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorFuseBoxInteract (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | f83206ec-16ae-424e-9c40-c283cdcf4e4b | c04_objective_ElevatorCableBoxInteract | Boolean | False | Objective tracker: marks that the player interacted with elevator cable box. | Written by `600_ElevatorCableBoxInteractSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorCableBoxInteract true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorCableBoxInteract (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | f7cdbf8b-eb6f-41b1-ba49-5ba47dd29d57 | c04_objective_ElevatorCableHintsRead | Boolean | False | Objective tracker: tracks that the player read/used the clue for elevator cable hints read. | Written by `600_ElevatorCableHintSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_CableHintsRead true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_CableHintsRead (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | c7004b40-85bc-4d0a-a274-05d771d581ab | c04_objective_ElevatorFuseGetInventory | Boolean | False | Objective tracker: tracks inventory/UI state for elevator fuse get inventory. | Written by `600_ElevatorFuseInventorySetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorFuseGetInventory true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorFuseGetInventory (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 8dc1c235-4ffc-4894-bd45-ae1cf2e5fba2 | c04_objective_ElevatorCableGetInventory | Boolean | False | Objective tracker: tracks inventory/UI state for elevator cable get inventory. | Written by `600_ElevatorCableInventorySetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorCableGetInventory true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorCableGetInventory (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 54cbb0b6-3e67-4a8b-8751-7096edd327a2 | c04_objective_ElevatorFuseCableSet | Boolean | False | Objective tracker: tracks elevator fuse cable set. | Written by `800_ElevatorFuseCableSetSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_ElevatorFuseCableSet true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_ElevatorFuseCableSet (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 2f650111-b536-4eac-9e1d-a2bab0749a88 | c04_objective_NoMisshon | Boolean | False | Objective tracker: toggles no mission. | Written by `100_EathanRescue` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_NoMisshon true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_NoMisshon (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 9117fac8-2a65-4ff3-9427-692c23fa7825 | c04_objective_PlChange | Boolean | False | Objective tracker: tracks player change. | Written by `200_PlChange` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_PlChange true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_PlChange (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 91b7b0b4-4f0e-43f0-8001-1567aab6fbe0 | c04_objective_HutCommunicatorOrMineEnter | Boolean | False | Objective tracker: marks that the player entered hut communicator or mine. | Written by `200_WirelessInterceptSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_HutCommunicatorOrMineEnter true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_HutCommunicatorOrMineEnter (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | b533f399-5e1c-407e-a6bb-62b4cd7603a2 | c04_objective_EnecrotoxinAcsses | Boolean | False | Objective tracker: tracks E-Necrotoxin access. | Written by `400_NecrotoxinAccesseSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_EnecrotoxinAcsses true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_EnecrotoxinAcsses (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | a787b236-b098-4b69-93b2-6df65c97bbe6 | c04_objective_EnecrotoxinGet | Boolean | False | Objective tracker: marks that the player obtained E-Necrotoxin. | Written by `400_SerumTypeEGet` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_EnecrotoxinGet true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_EnecrotoxinGet (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 6fb8a14d-bc32-41eb-94f5-d51bea49f203 | c04_objective_EnecrotoxinUse | Boolean | False | Objective tracker: tracks E-Necrotoxin use. | Written by `50_SerumUsed` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_EnecrotoxinUse true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_EnecrotoxinUse (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 085e7235-51b0-4ae3-87e5-459d42538018 | c04_objective_Ending | Boolean | False | Objective tracker: tracks ending. | Written by `600_Ending` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_Ending true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_Ending (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 7928ad66-97e8-45ae-844b-bc5b057fc9fd | c04_objective_MachinegunGet | Boolean | False | Objective tracker: marks that the player obtained machinegun. | Written by `500_MachineGunGetSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_MachinegunGet true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_MachinegunGet (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | b37a2c35-db76-4b46-833a-0aea07ac8ac8 | c04_objective_MachinegunLockerSeach | Boolean | False | Objective tracker: marks that machinegun locker seach is unlocked. | Written by `500_LockerInteractSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_MachinegunLockerSeach true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_MachinegunLockerSeach (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | b8ca8b46-127f-4598-92fa-0fa6597d323f | c04_objective_LockerKey_Get | Boolean | False | Objective tracker: marks that the player obtained locker key. | Written by `500_LockerKeyGet_SetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_LockerKey_Get true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_LockerKey_Get (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 739c0eae-88da-4df4-a51f-7e0fcdb81b19 | c04_objective_MachinegunLockerOpen | Boolean | False | Objective tracker: marks that machinegun locker open is unlocked. | Written by `500_LockerOpen_SetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_MachinegunLockerOpen true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_MachinegunLockerOpen (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | a888c83f-a23e-4e2e-8be9-1483917ce54f | c04_objective_DissolvingAgentDoorOpen | Boolean | False | Objective tracker: marks that corrosive agent door open is open. | Written by `500_DissolvingAgentDoorOpenSetBool` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_DissolvingAgentDoorOpen true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_DissolvingAgentDoorOpen (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |
| Likely | 77cc0c9b-f270-49a0-bc9c-d39c8295ff05 | c04_objective_DissolvingAgentDoorInteract | Boolean | False | Objective tracker: marks that the player interacted with corrosive agent door. | Written by `500_DissolvingAgentDoorInteract` in `objectivefsm_c04.scn.20`. | Write via `via.fsm.action.SetBool`: sets c04_objective_DissolvingAgentDoorInteract true (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c04_objective_DissolvingAgentDoorInteract (`leveldesign/fsm/chapter4/objectivefsm_c04.scn.20`) |

### FF000_Objective_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 0fb0e55d-1ef4-4147-a90a-e5196d0faffd | FF000_objective_PeteSaysWheresAndre | Boolean | False | Objective tracker: tracks pete says wheres andre. | Written by `300_KitchenAndLiving` in `npc_ff000.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF000_objective_PeteSaysWheresAndre true (`leveldesign/fsm/ff000/npc/npc_ff000.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF000_objective_PeteSaysWheresAndre (`leveldesign/fsm/ff000/objectivefsm_ff000.scn.20`) |

### FF050_Objective_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | e533965f-c5af-4122-8418-67e48c44f418 | FF050_objective_SeeEvline2F | Boolean | False | Objective tracker: tracks see Eveline 2F. | Written by `300_SeeEvline2F` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF050_objective_SeeEvline2F true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF050_objective_SeeEvline2F (`leveldesign/fsm/ff050/objectivefsm_ff050.scn.20`) |
| Likely | 8b7b9a98-37c6-427b-b0da-7a52b1dcd116 | FF050_objective_AlanSayFoundSpecimenBox | Boolean | False | Objective tracker: tracks alan say found specimen box. | Written by `400_AlanSayFound` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF050_objective_AlanSayFoundSpecimenBox true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF050_objective_AlanSayFoundSpecimenBox (`leveldesign/fsm/ff050/objectivefsm_ff050.scn.20`) |
| Likely | 5025fec3-e3bc-42ec-80e0-aaca08e5e981 | FF050_objective_DoorOpenBySolvents | Boolean | False | Objective tracker: marks that door open by corrosive is open. | Written by `450_DoorCheck_SetBool` in `level_fsm_ff050.scn.20`. | Write via `via.fsm.action.SetBool`: sets FF050_objective_DoorOpenBySolvents true (`leveldesign/fsm/ff050/level_fsm_ff050.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=FF050_objective_DoorOpenBySolvents (`leveldesign/fsm/ff050/objectivefsm_ff050.scn.20`) |

### Tips_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 9efe6073-a652-432e-95f8-05cfd80a1160 | Tips_SlowMolded | Boolean | False |  |  |  |
| Unknown | 9433d28f-4b2a-4aa6-b85e-0146d95c8696 | Tips_QuickMolded | Boolean | False |  |  |  |
| Unknown | a4655d4c-c851-44bd-9356-49f85f65e225 | Tips_FatMolded | Boolean | False |  |  |  |
| Unknown | 75aac5b5-ee67-4667-87f3-9d5c996c80ad | Tips_LegCutJapan | Boolean | False |  |  |  |
| Unknown | 8f577151-34ac-4f54-b2eb-56235da5f734 | Tips_LegCutForeign | Boolean | False |  |  |  |
| Unknown | 84e2406d-9811-4d6a-99ca-0fdfcd4f1307 | Tips_GrappleCounter | Boolean | False |  |  |  |
| Unknown | eaf19f08-38a5-4d07-afdd-a65ef1d0af08 | Tips_KnifeMia | Boolean | False |  |  |  |
| Unknown | 59bcf761-fc54-4f9b-8809-8c6ad73839c4 | Tips_ChainsawMia | Boolean | False |  |  |  |
| Unknown | fb5c4bb9-1ecb-4257-afe8-031f223b62fe | Tips_ShovelJack | Boolean | False |  |  |  |
| Unknown | d3aed37f-0fa2-4f71-b044-f6e8ea9e77a3 | Tips_GarageJack | Boolean | False |  |  |  |
| Unknown | b7c12e94-313e-48b8-bdd0-dfb9d2201240 | Tips_RollerJack | Boolean | False |  |  |  |
| Unknown | f1658e4f-a843-43a6-96ea-a08ff0a2a239 | Tips_ScissorsJack | Boolean | False |  |  |  |
| Unknown | 09e6a94c-e038-4eb6-928e-a2bd32e8961c | Tips_PursuitMarguerite | Boolean | False |  |  |  |
| Unknown | 588df742-2ae9-4ec1-8ec9-b7afa8725956 | Tips_PitFight | Boolean | False |  |  |  |
| Unknown | a2a96254-1496-4852-b1e4-9af17098252d | Tips_MutatedMarguerite | Boolean | False |  |  |  |
| Unknown | 333d289f-09ba-4432-af60-1a28e3053f53 | Tips_Passcodes | Boolean | False |  |  |  |
| Unknown | 506d7c96-70bc-4264-ae9e-24e3545f2004 | Tips_FatMoldedBoss | Boolean | False |  |  |  |
| Unknown | d14f7b01-1873-426d-bc26-d235534fd90e | Tips_MutatedJack | Boolean | False |  |  |  |
| Unknown | df800b38-8472-409d-834f-c04929265965 | Tips_EvelineNecrotoxin | Boolean | False |  |  |  |
| Unknown | 5689a7d3-0eee-4379-a701-7ce2ed4245c8 | Tips_EvelineWall | Boolean | False |  |  |  |
| Unknown | 5b9f875f-207b-435f-8dca-3eaf041d40cb | Tips_EvelineAlbert | Boolean | False |  |  |  |
| Likely | 85580e3f-7d04-4a2b-8034-3439eb97f9c9 | Tips_PartyRoom | Boolean | False | Tips state: tracks tips party room. | Written by `1280_BumbTimeCountStart` in `puzzle_gimmick.scn.20`. | Write via `via.fsm.action.SetBool`: sets Tips_PartyRoom true (`leveldesign/fsm/chapter3/chapter3_4/puzzle_gimmick.scn.20`) |
| Unknown | 378d1c4f-4b0d-46b5-8cb7-d639544589fb | Tips_ScorpionKey | Boolean | False |  |  |  |

### TelemetryCollectableStats

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 0971e39f-7c5c-48ea-a544-7c41062cc0f9 | Cleared_Prologue | Boolean | False |  |  |  |
| Unknown | 488b6d5b-7957-4d88-b96e-a9c6c1b991e5 | Cleared_Chapter1 | Boolean | False |  |  |  |
| Unknown | 0611eed5-0d5b-4e34-a843-29f4b44ec941 | Cleared_Garage | Boolean | False |  |  |  |
| Unknown | 865f0cf4-ff5c-46df-a046-7325db1c222e | Cleared_Scissor | Boolean | False |  |  |  |
| Unknown | bf16168b-6ae7-46a4-8744-df90cb7325ca | Cleared_Greenhouse | Boolean | False |  |  |  |
| Unknown | cb062f26-10cb-41d5-ba0e-9df37816780c | Cleared_Partyroom | Boolean | False |  |  |  |
| Unknown | 999449a2-4a27-43b8-a57c-c60ac5332858 | Cleared_Chapter3 | Boolean | False |  |  |  |
| Unknown | 3eb9911a-82ca-4463-bbc6-f94a03a4df8f | Cleared_Ship | Boolean | False |  |  |  |
| Unknown | 8f3f4741-2560-4c68-9fc1-8e43ff2cf782 | Cleared_Chapter4 | Boolean | False |  |  |  |
| Unknown | ae04da1f-854b-409a-b3a3-d743f5e526f4 | Cleared_FF1 | Boolean | False |  |  |  |
| Unknown | de07d2a4-302e-440a-9910-515686117b6c | Cleared_FF2 | Boolean | False |  |  |  |
| Unknown | 0fb32ee7-8a6e-4490-9441-7f4227bd8c28 | Cleared_FF3 | Boolean | False |  |  |  |
| Unknown | 1512a162-71a2-4d82-966f-955a2d3ad49a | Cleared_FF4 | Boolean | False |  |  |  |

### TelemetryCountableStats

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 61c4a56a-0a01-46eb-bb6a-b0aa103a5423 | Dead_Em2000 | Uint32 | 0 |  |  |  |
| Unknown | 762a7b95-5154-47a4-8095-6a0efafb8b87 | Dead_Em3000 | Uint32 | 0 |  |  |  |
| Unknown | 78953ab0-5f58-41ca-818f-936323a7a796 | Dead_Em3600 | Uint32 | 0 |  |  |  |
| Unknown | 507ba8ea-cd10-4588-818e-601aa12bf622 | Dead_Em4000 | Uint32 | 0 |  |  |  |
| Unknown | 2324920a-0874-42c8-b027-5b739f88b4a4 | Dead_Em4100 | Uint32 | 0 |  |  |  |
| Unknown | 00592069-b310-4723-bd7b-1f54819e47ee | Dead_Em4200 | Uint32 | 0 |  |  |  |
| Unknown | f561f1e8-9f64-4c07-a8af-233479221424 | Dead_Em8000 | Uint32 | 0 |  |  |  |
| Unknown | 6cc6b698-a9d7-4d5d-999e-5e93c27657be | Dead_Em8100 | Uint32 | 0 |  |  |  |
| Unknown | 4dffea7a-b259-473b-80ac-508b68f78ea6 | Dead_Em8940 | Uint32 | 0 |  |  |  |
| Unknown | 0577c2f0-359a-48ad-8107-f2e49c538e23 | Dead_Em5400 | Uint32 | 0 |  |  |  |
| Unknown | 9a08d379-4aab-44a0-ba14-e6a23d7da155 | Dead_Em5510 | Uint32 | 0 |  |  |  |
| Unknown | 249ba941-8c7c-4de1-be54-43afabea5d70 | Dead_Em5520 | Uint32 | 0 |  |  |  |
| Unknown | 370c74d1-49e0-4584-998b-b06b8cb5a216 | Dead_Em5540 | Uint32 | 0 |  |  |  |
| Unknown | ad52971b-1a5f-4da7-a907-be83bd1d2733 | Dead_Em5552 | Uint32 | 0 |  |  |  |
| Unknown | c29b0aed-55fa-4680-9672-ba481495f7b8 | Dead_Em5570 | Uint32 | 0 |  |  |  |
| Unknown | d6dbc837-8721-41ce-bed9-e56c33bf959d | Used_Wp0040 | Uint32 | 0 |  |  |  |
| Unknown | d1b2621c-9e33-4abb-ab37-3fe1d0b25134 | Used_Wp1010 | Uint32 | 0 |  |  |  |
| Unknown | c1e0233e-3bc0-4e3a-ae36-1ab8d2c43061 | Used_Wp1190 | Uint32 | 0 |  |  |  |
| Unknown | 98ee81f1-7848-46e4-92c1-acb9616a3204 | Used_Wp1210 | Uint32 | 0 |  |  |  |
| Unknown | c743e7b0-f6cd-4544-a675-339b58f2c876 | Used_Wp1230 | Uint32 | 0 |  |  |  |
| Unknown | c18432c7-1209-4fa0-8817-a1433828bcd1 | Used_Wp1000 | Uint32 | 0 |  |  |  |
| Unknown | fcb166e3-3da3-4137-98f2-9ababb2d38df | Used_Wp1110 | Uint32 | 0 |  |  |  |
| Unknown | 253d6197-d8e2-4648-a626-2b0d67ef5a34 | Used_Wp1140 | Uint32 | 0 |  |  |  |
| Unknown | 806b54ce-f919-4c3a-9202-6ab23f94070a | Used_Wp1240 | Uint32 | 0 |  |  |  |
| Unknown | 5dc5e8f9-aed9-48f1-954a-55e54ecdcb07 | Used_Wp1160 | Uint32 | 0 |  |  |  |
| Unknown | afee02f7-4334-4733-97a1-dc11c1920945 | Used_Wp1340 | Uint32 | 0 |  |  |  |
| Unknown | 5fb151c3-6498-40f2-a4dc-b977d4e92c08 | Used_Wp1280 | Uint32 | 0 |  |  |  |
| Unknown | cbf9edc8-3f9e-42af-9cf6-b29651204bb3 | Used_Wp1320 | Uint32 | 0 |  |  |  |
| Unknown | ff041586-10ac-4d3a-8031-1b55d97d5db6 | Used_Wp0060 | Uint32 | 0 |  |  |  |
| Unknown | 70dae890-622b-4ad3-a952-cb476f4990a6 | Used_Wp1030 | Uint32 | 0 |  |  |  |
| Unknown | 350f612a-603c-49c9-94b6-6406696f1a38 | Used_Wp1380 | Uint32 | 0 |  |  |  |
| Unknown | f39011b8-6b1c-4243-9cb4-90bc348e6c0f | Used_Wp1370 | Uint32 | 0 |  |  |  |
| Unknown | dc558458-16a3-4746-82c0-7d1122a754b4 | Used_Wp0070 | Uint32 | 0 |  |  |  |
| Unknown | 28ff3e85-5a9c-4fcd-823e-cd0c2f91d898 | Used_Wp1270 | Uint32 | 0 |  |  |  |
| Unknown | 7a6ad73c-1472-47fe-8b7f-e922a32d713d | Used_Wp1111 | Uint32 | 0 |  |  |  |
| Unknown | a03c6d36-faf6-4906-b23c-98d6c8f436f0 | Used_Wp1112 | Uint32 | 0 |  |  |  |
| Unknown | a883de0c-1239-4eb7-aea4-84f7c84019e2 | Used_Wp1013 | Uint32 | 0 |  |  |  |
| Unknown | c04a5e1f-483b-48d1-80a0-0a5410785064 | Used_Wp1011 | Uint32 | 0 |  |  |  |
| Unknown | 79923888-8026-4f6d-aef7-1047f99e73d4 | Used_WpReward | Uint32 | 0 |  |  |  |
| Unknown | 19c48b69-121b-49c4-8360-ddc1dcb362d0 | Used_Wp1381 | Uint32 | 0 |  |  |  |
| Unknown | 19906289-46c5-4031-8a91-45c1f13251e6 | Used_Wp1382 | Uint32 | 0 |  |  |  |
| Unknown | 1d3f43be-fac4-4ddc-bc21-9bc46ecdf0de | Used_Wp1360 | Uint32 | 0 |  |  |  |
| Unknown | 74023ab7-b3b7-42a0-ba6a-c49b34419cb0 | Used_ItemRemedyM | Uint32 | 0 |  |  |  |
| Unknown | 6bcedc36-7d85-4904-96bc-02ddef41b1f1 | Used_ItemRemedyL | Uint32 | 0 |  |  |  |
| Unknown | e9907121-8987-43a3-a6c6-834330cc5991 | Used_ItemEyeDrops | Uint32 | 0 |  |  |  |
| Unknown | 601d9626-698e-46e2-ac14-88f38592c2e4 | Used_ItemStimulant | Uint32 | 0 |  |  |  |
| Unknown | 1bc0421d-8f75-4365-a1c7-ab217ad9c3ff | Used_ItemDepressant | Uint32 | 0 |  |  |  |
| Unknown | e4b729c4-2fb4-40b6-a964-0b6d1295d60c | Used_ItemGunpowder | Uint32 | 0 |  |  |  |
| Unknown | ecd9d3fa-52e1-4823-b1ea-887058c7e46c | Used_ItemChemicalM | Uint32 | 0 |  |  |  |
| Unknown | 30f0a644-8fe8-4b94-ac79-050bdd6b86ff | Used_ItemChemicalL | Uint32 | 0 |  |  |  |
| Unknown | aa72fb43-8187-41ba-9d53-9bcef38437c8 | Formulated_ItemRemedyM | Uint32 | 0 |  |  |  |
| Unknown | a3059635-c85c-4628-8dbd-1e80e897e3cf | Formulated_ItemRemedyL | Uint32 | 0 |  |  |  |
| Unknown | 27bfca8a-2d6a-437c-884c-ce47f145c157 | Formulated_ItemEyeDrops | Uint32 | 0 |  |  |  |
| Unknown | b0943a93-3409-4a4f-8f61-baecbb6478a9 | Formulated_ItemHandgunBullet | Uint32 | 0 |  |  |  |
| Unknown | 90700fa5-834b-4f56-be64-dd9940d71690 | Formulated_ItemFlameBullet | Uint32 | 0 |  |  |  |
| Unknown | b7a4f466-a5bc-47da-bd76-f984cc968c3e | Formulated_ItemAcidBullet | Uint32 | 0 |  |  |  |
| Unknown | 577cc131-4738-4703-8033-816178ea98d7 | Formulated_ItemBurnerBullet | Uint32 | 0 |  |  |  |
| Unknown | 9422213a-8485-45c2-914b-7631bfc9786c | Formulated_ItemHandgunBulletL | Uint32 | 0 |  |  |  |
| Unknown | 3d39731a-3c4c-4d87-930d-72d21147585a | Others_CountOfRepulsedInTheAir | Uint32 | 0 |  |  |  |
| Unknown | 8fc307e7-c132-4f4d-a4b7-49ecaf1c14c2 | Others_CountOfKilledByAttachBomb | Uint32 | 0 |  |  |  |
| Unknown | 8381f4ae-2a51-48bd-bfdb-913bd7310544 | Others_CountOfAvoidInTheSquat | Uint32 | 0 |  |  |  |
| Unknown | 410eaa52-a020-4ea3-9d2f-4854dff31138 | Others_CountOfBrokenInsectDoors | Uint32 | 0 |  |  |  |
| Unknown | 8681e7a8-2bb9-4217-812c-0ef33f6e7ed6 | Others_CountOfSucceededGuards | Uint32 | 0 |  |  |  |
| Unknown | 566a3045-6386-426c-ba3e-688e8cded6eb | Others_CountOfOpenItemBox | Uint32 | 0 |  |  |  |
| Unknown | 33e1ce17-61cb-44ee-a25b-9c726df7b581 | Others_CountOfCutPlayerLegs | Uint32 | 0 |  |  |  |
| Unknown | 9fcdbfc2-c914-4123-9d1e-e5a5e4e96a4c | Others_CountOfCutPlayerArms | Uint32 | 0 |  |  |  |
| Unknown | 6a336554-98a2-44cd-9e93-f3a5bb48866c | Others_CountOfOpenDoors | Uint32 | 0 |  |  |  |
| Unknown | d9b55587-ca21-4d60-bf58-b565435faf94 | Others_CountOfCloseDoors | Uint32 | 0 |  |  |  |
| Unknown | a9646b0e-ccf1-40ff-ac8c-0a7f676a08b1 | Others_CountOfSetupShadowPuzzle | Uint32 | 0 |  |  |  |
| Unknown | 38947acb-42f2-4c6c-b2f0-01239a061f86 | Others_CountOfChooseMia | Uint32 | 0 |  |  |  |
| Unknown | 4b197db1-417a-410b-8f93-f128181d4c62 | Others_CountOfChooseZoe | Uint32 | 0 |  |  |  |
| Unknown | 37907f96-0700-4ea7-8aa9-f140071a53f9 | Others_CountOfAttackedCar | Uint32 | 0 |  |  |  |
| Unknown | 7be4aab9-ce5a-4826-8264-6b8eefb82c95 | Others_CountOfDamagedCar | Uint32 | 0 |  |  |  |
| Unknown | 77d41c03-9192-41f8-8042-729827434f84 | Others_CountOfGenerateNest | Uint32 | 0 |  |  |  |
| Unknown | a78bc546-179c-49be-b90e-73c4f5f10b98 | Others_CountOfDamageTrap | Uint32 | 0 |  |  |  |
| Unknown | 6f98e561-8784-4eb0-9738-a302ea08a039 | Others_CountOfInteractBrassiere | Uint32 | 0 |  |  |  |
| Unknown | 92624fde-5a06-48bb-9ffd-c3d91c983646 | Others_CountOfDumpItem | Uint32 | 0 |  |  |  |
| Unknown | 65be4c31-7b13-4507-acc6-67c785207ea3 | Others_CountOfEatHerbs | Uint32 | 0 |  |  |  |
| Unknown | c8952171-0396-462b-afa7-a8f3da4a6db0 | Others_CountOfBilliards | Uint32 | 0 |  |  |  |
| Unknown | 5c04e888-55e6-4fc7-881f-f56ddbdbe5f9 | PlayerDisplacement | Single | 0 |  |  |  |
| Unknown | 38b7a171-4dbb-4792-b455-55026444bd5d | LostParts_Em4000Head | Uint32 | 0 |  |  |  |
| Unknown | db82f1f9-7418-4ff0-8dc4-b3b52b0ba061 | LostParts_Em4000LeftArm | Uint32 | 0 |  |  |  |
| Unknown | fdbf5d9b-dbd3-4502-bfd6-d0b30e695265 | LostParts_Em4000RightArm | Uint32 | 0 |  |  |  |
| Unknown | a54700c8-8a88-4c13-aef7-4ea87fa8fea0 | LostParts_Em4000LeftLeg | Uint32 | 0 |  |  |  |
| Unknown | 7810c60c-05fb-4c7e-8df9-a689854df816 | LostParts_Em4000RightLeg | Uint32 | 0 |  |  |  |
| Unknown | f5213e77-5e4b-4b1c-872b-ae9da8f4fb00 | LostParts_Em4100Head | Uint32 | 0 |  |  |  |
| Unknown | 25e0acfc-ba6f-4101-af7f-01a5066ca14f | LostParts_Em4200Head | Uint32 | 0 |  |  |  |
| Unknown | aa93c658-3aed-4056-a287-f6df6d0c992e | LostParts_Em4200LeftArm | Uint32 | 0 |  |  |  |
| Unknown | d9d446d5-9388-4712-8e05-3424165cfb6b | LostParts_Em4200RightArm | Uint32 | 0 |  |  |  |
| Unknown | b3d861fb-a278-49fc-822a-7abd717ff092 | LostParts_Em4200LeftLeg | Uint32 | 0 |  |  |  |
| Unknown | 5f468227-23d3-4f9c-9b13-e3df05815fc4 | LostParts_Em4200RightLeg | Uint32 | 0 |  |  |  |
| Unknown | 2f1e8f54-69c3-4ec9-8f26-dffed4b2da1a | GameOver_ByCaughtMother | Uint32 | 0 |  |  |  |
| Unknown | 2a09cf68-f6fa-4891-a2f3-a1b4d627eb97 | GameOver_ByCar | Uint32 | 0 |  |  |  |
| Unknown | 988c8549-875b-4e2c-8851-454685057798 | GameOver_ByTrapBomb | Uint32 | 0 |  |  |  |
| Unknown | 0e567d21-95c7-476b-821a-dcf8f6daf92f | GameOver_ByTrapPendulum | Uint32 | 0 |  |  |  |
| Unknown | 0b90fbf9-fe7d-4a9f-96d4-b83c62ef15f7 | GameOver_ByTrapCake | Uint32 | 0 |  |  |  |
| Unknown | 657c576b-601b-46fe-982e-58131e86a756 | GameOver_ByLegCut | Uint32 | 0 |  |  |  |
| Unknown | 353a114b-ce30-405d-a339-dea28eb6ea49 | GameOver_BySuicide | Uint32 | 0 |  |  |  |
| Unknown | 132ffcc2-24c4-48ba-b634-f50e50f2b94b | ClearTime_GarageBattle | Uint32 | 0 |  |  |  |
| Unknown | 3681fa2c-d339-44b0-939d-42e949e8c113 | ClearTime_ScissorBattle | Uint32 | 0 |  |  |  |
| Unknown | 3ee1c0e6-b95e-4097-85b3-a3fabcc5ebe6 | ClearTime_FianlFatherBattle | Uint32 | 0 |  |  |  |
| Unknown | f784c095-8a72-474e-ae58-4c66ca96b446 | ClearTime_FinalMotherBattle | Uint32 | 0 |  |  |  |
| Unknown | 88ad4f0c-249d-4f18-83fb-3caadc359eba | ClearTime_FinalBossBattle | Uint32 | 0 |  |  |  |
| Unknown | 6ac63b54-21e5-48b7-8810-1f461b200da2 | ClearTime_PartyRoom | Uint32 | 0 |  |  |  |
| Unknown | 6d980ddd-5706-494e-9b95-a4587a4e4fb7 | ClearTime_PartyRoomInFF | Uint32 | 0 |  |  |  |

### NarrativeSound_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 8bde153d-e5f6-43a8-b52a-9911f7a8f21d | TestNaraSE_00 | Boolean | False |  |  |  |
| Unknown | 6089dc80-9014-4b55-9157-bba567115a8c | TestNaraSE_01 | Boolean | False |  |  |  |
| Unknown | 9fd0f658-a125-4977-9313-b8ef54340786 | NarrativeSE_ch01_MiaHandBag | Boolean | False |  |  |  |
| Unknown | 930c00b5-5977-4d21-bf12-7ecbe9cd8371 | NarrativeSE_ch31_KeyPick | Boolean | False |  |  |  |
| Unknown | e9180a40-c91f-4cab-9bb4-5c837789873f | NarrativeSE_ch31_Plate | Boolean | False |  |  |  |
| Unknown | b875bc6e-d9e5-44d7-8880-d2f0b0cede7f | NarrativeSE_ch31_EvelineBoots | Boolean | False |  |  |  |
| Unknown | 947d5dec-8e0e-4dec-92b2-894247d8814e | NarrativeSE_ch32_Book | Boolean | False |  |  |  |
| Unknown | e2368324-7be0-4e66-96e7-474d7ecd2882 | NarrativeSE_ch32_JackMemo1 | Boolean | False |  |  |  |
| Unknown | c8ec773e-6125-4cbb-8d8b-e4fdcda784cb | NarrativeSE_ch32_TravisMemo | Boolean | False |  |  |  |
| Unknown | 14259ed4-3167-4f84-87f9-396ee1ef998b | NarrativeSE_ch32_IncineratorMemo | Boolean | False |  |  |  |
| Unknown | 57848d0d-d192-4c7a-9884-d522b084a203 | NarrativeSE_ch32_JackMemo2 | Boolean | False |  |  |  |
| Unknown | 095490a2-04e0-425a-8e71-1ce31aed7c74 | NarrativeSE_FF020_BOW | Boolean | False |  |  |  |
| Unknown | 69e474f4-4e4d-4f01-bdaf-8a320fb28ea2 | NarrativeSE_ch33_MargaretHandbook | Boolean | False |  |  |  |
| Unknown | b100a528-c91a-48ba-a610-406f3ec14f3a | NarrativeSE_ch33_EvelineSketch | Boolean | False |  |  |  |
| Unknown | 9b5858ab-e3d8-47d7-9e3f-1005177f01f8 | NarrativeSE_ch34_LucasTrophy | Boolean | False |  |  |  |
| Unknown | 2d714b60-2cdf-4de3-97f2-4423e53c9931 | NarrativeSE_ch34_LucasDiary | Boolean | False |  |  |  |
| Unknown | c5250fed-00d4-4bae-ad50-546b0aeb5eea | NarrativeSE_ch34_LucasDiaryContents | Boolean | False |  |  |  |
| Unknown | 94123d09-fa1c-48d7-aeed-5be11692606b | NarrativeSE_ch34_BodyMemo | Boolean | False |  |  |  |
| Unknown | cef12cc7-d495-4290-81ea-56803356a152 | NarrativeSE_ch34_Telescope | Boolean | False |  |  |  |
| Unknown | ae3dc6c0-1f5a-420e-b255-0a894b84a8b6 | NarrativeSE_ch34_FindBomb | Boolean | False |  |  |  |
| Unknown | 6aeb3421-2370-4823-82e4-1f67579c2a43 | NarrativeSE_ch35_SuicideNote | Boolean | False |  |  |  |
| Unknown | ee396180-ecce-4246-9eae-118973cdf013 | NarrativeSE_ch35_PC | Boolean | False |  |  |  |
| Unknown | 4492c3e3-2e57-4459-86a7-99c224498307 | NarrativeSE_ch35_Photo | Boolean | False |  |  |  |
| Unknown | f0b717f4-db79-455f-a859-7b5daecb6794 | NarrativeSE_ch32_KeyPick | Boolean | False |  |  |  |
| Unknown | 9efb8040-d2b0-424a-b678-cffb6ce343ac | NarrativeSE_ch34_DaveMessage_Z | Boolean | False |  |  |  |
| Unknown | 5b88a349-0ede-4ea5-8f86-881e8fefe5f1 | NarrativeSE_ch34_DaveMessage_D | Boolean | False |  |  |  |
| Unknown | 89cd8c78-63eb-4796-b58f-886e0412d2bc | NarrativeSE_ch01_FindFuse | Boolean | False |  |  |  |
| Unknown | 70c15a7a-6c73-4a0b-9d3c-bc9cc4f02023 | NarrativeSE_ch32_FindStatuette | Boolean | False |  |  |  |
| Unknown | c67a350d-7c80-4c85-8b2e-a89e39cd8ea5 | NarrativeSE_ch34_DaveSign | Boolean | False |  |  |  |

### c07_1_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | e19eaa4d-ed17-4b29-b82f-422b7972fe50 | c07_1_Main_StartSetingOK | Boolean | False | Bedroom DLC flow: tracks start seting ready. | Written by `0_StartSetingOK` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Main_StartSetingOK true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_200.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_1_Main_StartSetingOK (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | e33b2c5e-578d-4504-b7da-c69c3c455829 | c07_1_Main_BeforeIntroQuestion | Boolean | False | Bedroom DLC flow: toggles before intro question. | Written by `200_BeforeIntroQuestion` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Main_BeforeIntroQuestion true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_250.user.2` |
| Likely | 9c5f3159-4f99-4cba-9662-78584b09fa02 | c07_1_Main_MotherGoOut | Boolean | False | Bedroom DLC flow: tracks Marguerite go out. | Written by `250_MotherGoOut` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Main_MotherGoOut true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_280.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_1_Main_MotherGoOut (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | 7af74f6f-2b91-4b02-baa5-4689731dcabf | c07_1_Main_SkipFlagON | Boolean | False | Bedroom DLC flow: toggles skip flag on. | Written by `250_SkipEventFlagON` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Main_SkipFlagON true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_300.user.2` |
| Likely | 69fd3cf5-0065-4f43-a75d-7c8da406a851 | c07_1_Main_KnifeGet | Boolean | False | Bedroom DLC flow: marks that the player obtained knife. | Required by game-flow package `c07_1_main_400.user.2`. | Gate package `c07_1_main_400.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_1_Main_KnifeGet (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | 49ff65f4-91f6-4ecc-b592-5588c9554367 | c07_1_Main_UnderFloorIN | Boolean | False | Bedroom DLC flow: tracks under floor in. | Written by `300_UnderFloorIN` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Main_UnderFloorIN true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_500.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_1_Main_UnderFloorIN (`leveldesign/fsm/chapter7/chapter7_1/objectivefsm_c07_1.scn.20`) |
| Likely | 667401cb-21d2-4b97-97ed-833702002c59 | c07_1_Main_GameClear | Boolean | False | Bedroom DLC flow: marks that game clear has ended/cleared. | Written by `500_GameClear` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Main_GameClear true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_600.user.2` |
| Likely | 2cf06e6f-bff2-4e0f-a07d-2466ea672be9 | c07_1_GameEnd | Boolean | False | Bedroom DLC flow: marks that game has ended/cleared. | Written by `600_AfterRequestJump` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_GameEnd true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Gate package `c07_1_main_end.user.2` |

### c07_1_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 02cf9492-b780-4cf0-bb85-17876b0feb56 | True_GimmickPictureA | Boolean | False |  |  |  |
| Unknown | 10cd2a27-2231-4aec-8d39-1ac520cfa50e | True_GimmickPictureB | Boolean | False |  |  |  |
| Unknown | e51b90b7-3701-4740-8eea-bc0577d7ef0d | True_GimmickPictureC | Boolean | False |  |  |  |
| Unknown | 39e19f6e-0bc2-4ac6-b740-e09b62be8f96 | Put_FoodRoundTable | Boolean | False |  |  |  |
| Likely | 9dccfe32-d649-4913-b2c3-20bbf45a09e8 | c06_Main_1stGimmickRelease | Boolean | False | Bedroom DLC flow: tracks c 06 main 1 st gimmick release. | Written by `300_NextForkEvent_FlagON` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c06_Main_1stGimmickRelease true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c06_Main_1stGimmickRelease (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | a4fd63a6-15f1-4a59-875f-10f0c5ec81cd | c06_Main_2stGimmickRelease | Boolean | False | Bedroom DLC flow: tracks c 06 main 2 st gimmick release. | Written by `300_NextQuestionEvent_FlagON` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c06_Main_2stGimmickRelease true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c06_Main_2stGimmickRelease (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 45313b69-8419-47ad-90bf-e1bc3a25d0c9 | c06_Main_3stGimmickRelease | Boolean | False | Bedroom DLC flow: tracks c 06 main 3 st gimmick release. | Written by `300_KnifeGet_FlagON` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c06_Main_3stGimmickRelease true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=c06_Main_3stGimmickRelease (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | 18a83152-13a4-4678-b386-f883e446ea79 | ForkDamage | Boolean | False |  |  |  |
| Likely | 2165a534-0f69-42b0-83fd-6abe4b4d6bd1 | Put_FoodOilCan | Boolean | False | Bedroom DLC flow: tracks put food oil can. | Read by `300_Mother_Appear_Fork_BGM` in `levelfsm_c07_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Put_FoodOilCan (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | c2622348-d3fa-4c9e-90bd-87d7b1cc79c4 | PL_BedIn | Boolean | False | Bedroom DLC flow: tracks player bed in. | Written by `FSM_PlayerBedAction` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets PL_BedIn false (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | adab2ffc-6273-4587-b7cf-af0ecb811278 | Deduction1 | Boolean | False |  |  |  |
| Unknown | c332ff23-e9dd-4e69-9836-df471137b407 | Deduction2 | Boolean | False |  |  |  |
| Likely | 731b961f-ae23-4d2f-a845-2fb2a1ce490f | Deduction3 | Boolean | False | Bedroom DLC flow: toggles deduction 3. | Read by `300_Mother_Appear_Fork_BGM` in `levelfsm_c07_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=Deduction3 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | 81b821b8-feb2-4772-b062-4b76085f3190 | Put_ShadowPuzzleObject | Boolean | False |  |  |  |
| Unknown | 84db78ef-644a-4529-b34a-1a703c6f2c6c | Put_ShadowPuzzleFork | Boolean | False |  |  |  |
| Unknown | 840caa1a-7c1c-4ce7-8de8-610eb685adbd | Put_ShadowPuzzleLantern | Boolean | False |  |  |  |
| Unknown | 0ca83eba-5060-4437-881b-6f6b0fa4d1b5 | MotherGimickNGPointingOutType | Int32 | 0 |  |  |  |
| Likely | d7b20508-74d0-4be3-9cbc-941b05a37157 | QuestionNG | Boolean | False | Bedroom DLC flow: toggles question failure. | Written by `200_Mother_Appear_1st_2` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets QuestionNG true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=QuestionNG (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 9ca52e9a-526a-4bea-9d49-6c8335eb9f60 | MotherAttackOK | Boolean | False | Bedroom DLC flow: tracks Marguerite attack ready. | Written by `300_Mother_Appear_Common` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets MotherAttackOK true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 1d20d3bf-672c-46d3-a56e-53b2ff2a38b9 | MotherComingSoon | Boolean | False | Bedroom DLC flow: toggles Marguerite coming soon. | Written by `300_Setting` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets MotherComingSoon false (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=MotherComingSoon (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | 076138e7-ea6f-4421-9743-f68d4ddc8dac | MotherQuestionSelectNum | Int32 | 0 |  |  |  |
| Unknown | c1e31212-a203-4f41-8b15-24a57777dc5c | MotherKnifeEvent | Boolean | False |  |  |  |
| Likely | 16bfe351-e3b8-4849-8aa0-36b30c2f0203 | RestraintBeltReturn | Boolean | False | Bedroom DLC flow: tracks restraint belt return. | Written by `Fsm_RestraintBeltReturn_FlagOFF` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets RestraintBeltReturn true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=RestraintBeltReturn (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 2d33282c-ef61-4664-aabc-aec157a2f313 | c07e11_50_Type | Uint32 | 0 | Bedroom DLC flow: tracks c 07 e 11 50 type. | Written by `Question_02` in `levelfsm_c07_1.scn.20`. | Write via `app.fsm.IntSet`: sets int to 200 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 67f3ecc5-c0c7-4365-87e1-e98d6eb10f08 | c07e11_60_Type | Uint32 | 0 | Bedroom DLC flow: tracks c 07 e 11 60 type. | Written by `Question_02` in `levelfsm_c07_1.scn.20`. | Write via `app.fsm.IntSet`: sets int to 200 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 2c8c6923-911e-44de-963c-3b8af878eb10 | c07e11_70_Type | Uint32 | 0 | Bedroom DLC flow: tracks c 07 e 11 70 type. | Written by `Question_02` in `levelfsm_c07_1.scn.20`. | Write via `app.fsm.IntSet`: sets int to 210 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 3abbd0fe-ad92-45c7-ada6-40f5630d6399 | c07e11_90_Type | Uint32 | 0 | Bedroom DLC flow: tracks c 07 e 11 90 type. | Written by `Question_02` in `levelfsm_c07_1.scn.20`. | Write via `app.fsm.IntSet`: sets int to 200 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | e7bebd1c-8df7-4f1b-8b5a-94e3e89ad472 | c07e11_91_Type | Uint32 | 0 | Bedroom DLC flow: tracks c 07 e 11 91 type. | Written by `300_Mother_Appear_Common` in `levelfsm_c07_1.scn.20`. | Write via `app.fsm.IntSet`: sets int to 100 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 6f901bcd-a27a-42f5-a604-780f525492f9 | FoundAllMouse | Int32 | 0 | Bedroom DLC flow: stores the numeric state for found all mouse. | Read by `Fsm_Achievement_MouseGet` in `levelfsm_c07_1.scn.20`. | Read via `app.fsm.IntTest`: compares int to 3 with compare type 0 (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 6af8119e-f16f-4892-9914-8b24b1dc4454 | c07_1_SkipStartEvent | Boolean | False | Bedroom DLC flow: tracks skip start event. | Written by `280_AutoSave_StartFsm` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_SkipStartEvent true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=c07_1_SkipStartEvent (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | 9bf0056f-bb6f-43d1-bfcb-dcc5105608cc | c07_1_Tips_FoodEat | Boolean | False |  |  |  |
| Likely | 3d08c5cb-a81c-4665-b383-ebd56fa11993 | c07_1_Tips_SoundComeBack | Boolean | False | Bedroom DLC flow: tracks tips sound come back. | Written by `c07_1_Tips_SoundComeBack` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_SoundComeBack true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 21fb3b16-7814-4cfc-bcca-9ffb1c05cb2f | c07_1_Tips_FurnitureReinstate | Boolean | False | Bedroom DLC flow: tracks tips furniture reinstate. | Written by `c07_1_Tips_FurnitureReinstate` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_FurnitureReinstate false (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | a3acf0f2-2e14-4e05-95e0-a61ff7c2db22 | c07_1_Tips_QuestionAnswer | Boolean | False |  |  |  |
| Likely | 852418ee-7e04-4ce2-9eaa-fc864b02af79 | c07_1_Tips_PicturePuzzleOrder | Boolean | False | Bedroom DLC flow: tracks puzzle progress for tips picture puzzle order. | Written by `c07_1_Tips_PicturePuzzleOrder` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_PicturePuzzleOrder true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 7ab20de1-f8fa-41c0-b4a8-9f0e8d7c41a2 | c07_1_Tips_SignLockHint | Boolean | False | Bedroom DLC flow: tracks that the player read/used the clue for tips sign lock hint. | Written by `c07_1_Tips_SignLockHint` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_SignLockHint true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | ad46db2c-28d1-4079-a4fe-ffb38cda175a | c07_1_Tips_JacketMemoPosition | Boolean | False | Bedroom DLC flow: tracks that the player read/used the clue for tips jacket memo position. | Written by `c07_1_Tips_JacketMemoPosition` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_JacketMemoPosition true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 8b015986-928a-4292-bd22-26f4b013b50a | c07_1_Tips_KnifeCache | Boolean | False | Bedroom DLC flow: tracks tips knife cache. | Written by `c07_1_Tips_KnifeCache` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_KnifeCache true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 59873b30-488e-4a8e-b686-1d7a8f5414a4 | c07_1_Tips_ShadowObjectFork | Boolean | False | Bedroom DLC flow: tracks puzzle progress for tips shadow object fork. | Written by `c07_1_Tips_ShadowObjectFork` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_1_Tips_ShadowObjectFork true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Unknown | 75c7065b-1417-4e68-a9b4-43bce1bcf366 | c07_1_Tips_KnifeEquip | Boolean | False |  |  |  |
| Likely | 68e8c14e-4e2a-4fac-b8d1-dd353272e4b5 | BeforeLastBattle | Boolean | False | Bedroom DLC flow: tracks before last battle. | Written by `300_Mother_Appear_Quesiton` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets BeforeLastBattle true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | f1799b5a-04d3-4ba3-ae21-dde5526992b4 | KnifeDamageLoopStart | Boolean | False | Bedroom DLC flow: marks that knife damage loop has started. | Read by `300_Mother_Appear_Dead_mes` in `levelfsm_c07_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=KnifeDamageLoopStart (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 4a93b205-ca49-4bbb-bd07-a9babdfefa1c | DiamondDoorKeyGet | Boolean | False | Bedroom DLC flow: marks that the player obtained diamond door key. | Written by `300_NextLastEvent_FlagON` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets DiamondDoorKeyGet true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | c87185fd-c005-4692-b918-a02f6f0979b8 | DirtyPotEat | Boolean | False | Bedroom DLC flow: tracks dirty pot eat. | Written by `300_DirtyPotEat_FlagON` in `levelfsm_c07_1.scn.20`. | Write via `via.fsm.action.SetBool`: sets DirtyPotEat true (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=DirtyPotEat (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |
| Likely | 4d7d9936-fde6-456c-b9de-6f753480ae27 | LastKnifeEventStart | Boolean | False | Bedroom DLC flow: marks that last knife event has started. | Read by `500_DoorClose_MotherOff` in `levelfsm_c07_1.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=LastKnifeEventStart (`leveldesign/fsm/chapter7/chapter7_1/levelfsm_c07_1.scn.20`) |

### ProgressiveNumber_c07_1

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | a2f45f94-2e9f-4584-954d-ca9afef2b1b8 | c07_1_Main | Int32 | 0 |  |  |  |

### ProgressiveNumber_c07_3

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 3752d4f1-6947-437c-99b1-913e1541db28 | c07_3_Main | Int32 | 0 |  |  |  |

### ProgressiveNumber_c08

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 6252ea7a-398e-4ffa-9839-d01d55a74e19 | c08_Main | Int32 | 0 |  |  |  |

### ProgressiveNumber_c07_4

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 9ecd34c2-f72d-43c8-b9ba-a67eb238fdee | c07_4_Main | Int32 | 0 |  |  |  |

### StoreItem_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | fecad200-5180-4adc-ae53-d9338f70d6f6 | Store_Z_Buy | Boolean | False |  |  |  |
| Unknown | b0c44a3b-0383-4348-8510-571e26771a1d | SupplyBoxA_Got | Boolean | False |  |  |  |
| Unknown | cd7c9b13-cb52-40df-b188-6b7f549eef0f | GoodLuckCoinA_Got | Boolean | False |  |  |  |
| Unknown | 9e5d861a-ca19-4479-89c2-1dbb43a30c24 | SupplyBoxB_Got | Boolean | False |  |  |  |
| Unknown | 885c4717-db73-4ae5-ba31-8c0536f1b77c | GoodLuckCoinB_Got | Boolean | False |  |  |  |
| Unknown | 1dfd6465-bb6b-4d3a-b515-33279a928d17 | SupplyBoxC_Got | Boolean | False |  |  |  |
| Unknown | 7b4c153c-9979-45a6-b9e4-7fd6a9bc3383 | GoodLuckCoinC_Got | Boolean | False |  |  |  |
| Unknown | bc3b5096-54d7-4c1e-8dde-92fb0aa16f7c | SupplyBoxD_Got | Boolean | False |  |  |  |
| Unknown | 89b76f08-6abb-4834-aafd-9cf405e4b31c | GoodLuckCoinD_Got | Boolean | False |  |  |  |
| Unknown | 1e2fcc34-2ad3-4aa1-93cf-b639df787fc6 | SupplyBoxE_Got | Boolean | False |  |  |  |
| Unknown | ce7aa28d-57f9-4f70-8328-2da33eff87fa | GoodLuckCoinE_Got | Boolean | False |  |  |  |
| Unknown | fecddd74-991d-4244-8b6c-669e9a7a70fb | SupplyBoxG_Got | Boolean | False |  |  |  |

### StoreItemUsed_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 35f02881-7574-41ba-8cd2-b1741fd8e8d1 | SupplyBoxA_Used | Boolean | False |  |  |  |
| Unknown | 634ed505-709a-45e4-bef7-acfac7de2c52 | SupplyBoxB_Used | Boolean | False |  |  |  |
| Unknown | 65523680-34cb-4d97-893d-7c9875d7724c | SupplyBoxC_Used | Boolean | False |  |  |  |
| Unknown | 0c763123-0bed-41dd-8c2c-b3491761e323 | SupplyBoxD_Used | Boolean | False |  |  |  |
| Unknown | 00efa424-8f38-48c3-aaa5-f8d82f68de35 | SupplyBoxE_Used | Boolean | False |  |  |  |
| Unknown | 5758b197-aca1-4c1c-b7f7-6aede572d256 | SupplyBoxG_Used | Boolean | False |  |  |  |

### Objective_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | c3d28853-5d47-4637-8a50-c86d5e19a37f | Main_FindMia_O | Boolean | False | Objective tracker: tracks main find Mia o. | Written by `Tutorial_Objective` in `c01_tutorial.scn.20`. | Write via `via.fsm.action.SetBool`: sets Main_FindMia_O true (`leveldesign/fsm/chapter1/c01_tutorial.scn.20`) |
| Unknown | 336a2867-21ae-46b6-a33d-bb95d0e4429e | Main_EscapeFromFamily_O | Boolean | False |  |  |  |

### RewardUnlocked_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | b3dae8ad-c363-4211-a4e0-fd9f3bc9a93a | ClearCasualOrNormal_Unlocked | Boolean | False |  |  |  |
| Unknown | e298e82f-362b-46ee-8e86-e902b853864b | ClearNormal_Unlocked | Boolean | False |  |  |  |
| Unknown | dc8ef8cb-461d-44d2-bfcb-8fae400f1fd7 | ClearHard_Unlocked | Boolean | False |  |  |  |
| Unknown | 17ae5fe7-be17-47e6-8eb3-5f9ab5ad1c7e | DestoryMrEveryWhere_Unlocked | Boolean | False |  |  |  |
| Unknown | 13443849-01a0-48e2-8ae4-4be24f680579 | ClearTimeAttack_Unlocked | Boolean | False |  |  |  |
| Unknown | 26cc2a40-c846-4af3-8583-d4c1fe95d207 | Birthday_ALL_S_Unlocked | Boolean | False |  |  |  |
| Unknown | 7d188559-3f64-4422-ab16-e7f0f37ce97e | Hard_Unlocked | Boolean | False |  |  |  |
| Unknown | 4795f4e8-6849-497b-a654-53756cf6ebdf | ClearTU2_Unlocked | Boolean | False |  |  |  |

### RewardGot_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 342db1cd-e2f6-42f6-9af9-6aee986aaa14 | ClearCasualOrNormal_Got | Boolean | False |  |  |  |
| Unknown | 292fc185-3c63-4a38-9042-4fd3eba8aeb2 | ClearNormal_Got | Boolean | False |  |  |  |
| Unknown | 42370a82-9cf5-4455-9dd0-edbf6df6072a | ClearHard_Got | Boolean | False |  |  |  |
| Unknown | 38d143be-1c3e-403d-a7cd-06aa885051f5 | DestoryMrEveryWhere_Got | Boolean | False |  |  |  |
| Unknown | 30706212-7e10-4536-a8f2-fef12ae90983 | ClearTimeAttack_Got | Boolean | False |  |  |  |
| Unknown | 1a5b7bda-a050-4920-9293-e64120b1bd36 | Birthday_ALL_S_Got | Boolean | False |  |  |  |
| Unknown | f3da79d7-d08d-400b-a544-92c0494052f2 | ClearTU2_Got | Boolean | False |  |  |  |

### c04_3_LastBoss_Event_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | ca8fd100-2ec6-4539-8cb6-88a5bead7086 | bFootWorkEnd | Boolean | False |  |  |  |
| Likely | 24d9692f-be34-4a6e-8d0e-c5de8240c08b | bGunCaseDropStart | Boolean | False | Salt mine and ending flow: marks that b gun case drop has started. | Read by `100_LastBossUnknownCommu` in `levelfsm_c04_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=bGunCaseDropStart (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`) |
| Unknown | 4fec8350-ee30-441f-8661-51a3efe5b142 | bGetAlbert | Boolean | False |  |  |  |
| Likely | 888ac22b-5ffa-4d50-a2b5-8acc3fd1f975 | bLastBossPlayerDead | Boolean | False | Salt mine and ending flow: marks death/failure state for b last boss player dead. | Read by `100_LastBossAtticBattleStart` in `levelfsm_c04_3.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=bLastBossPlayerDead (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`) |
| Unknown | 340af74a-095e-4625-bd00-9c35b7e4e925 | bToApproach3 | Boolean | False |  |  |  |
| Unknown | b5677bbb-3f47-4c96-9162-639ffcd52b2b | uFaceDamageCount | Uint32 | 0 |  |  |  |
| Unknown | 4bf52566-e998-43c8-a526-69a7338b8a02 | bFaceDamage1 | Boolean | False |  |  |  |
| Unknown | ceeab3da-7838-4c7e-b949-9896a13ef735 | bFaceDamage2 | Boolean | False |  |  |  |
| Unknown | cc9221d9-c888-4c7a-a45e-434f334af1c0 | bFaceDead | Boolean | False |  |  |  |
| Likely | d7a784ba-3c6f-4aed-b1ef-624c88c7a97c | bStopEnvEffect | Boolean | False | Salt mine and ending flow: tracks b stop env effect. | Written by `100_LastBossStopEnvEffect` in `levelfsm_c04_3.scn.20`. | Write via `via.fsm.action.SetBool`: sets bStopEnvEffect false (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=bStopEnvEffect (`leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20`) |
| Unknown | 5a8c91e8-4013-4471-9e96-6a7c4e20cbeb | bPlayerHangUpDead | Boolean | False |  |  |  |

### ChapterOver_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | a9359e9a-07c9-4bf9-9f33-bc0b0a9f3029 | BirdcageCoinNum0 | Int32 | 0 |  |  |  |
| Unknown | 6e15587e-ef2b-447d-917d-e8c6b1685a2a | BirdcageItemGet0 | Boolean | False |  |  |  |
| Unknown | 31b77875-606a-44c0-9a60-69d125cce2ef | BirdcageCoinNum1 | Int32 | 0 |  |  |  |
| Unknown | 609c7728-90b3-4943-83e5-3ffca386030c | BirdcageItemGet1 | Boolean | False |  |  |  |
| Unknown | a7b11cd1-408a-4561-bb8d-41cde2939f10 | BirdcageCoinNum2 | Int32 | 0 |  |  |  |
| Unknown | f2f0a3a2-f7ee-4df1-b11a-3c0ff8ff636d | BirdcageItemGet2 | Boolean | False |  |  |  |
| Unknown | 04d8da6d-77cc-41d9-9ddc-c69c28fcfcee | BirdcageCoinNum3 | Int32 | 0 |  |  |  |
| Unknown | 87d93eed-d072-4bbf-b992-bef86f7ebae0 | BirdcageItemGet3 | Boolean | False |  |  |  |
| Unknown | 54f4ee60-f69c-493b-ab2d-71cedfe30f2d | BirdcageCoinNum4 | Int32 | 0 |  |  |  |
| Unknown | 22cbc679-c790-440d-8dfd-6695ba6f4c66 | BirdcageItemGet4 | Boolean | False |  |  |  |
| Unknown | 214712be-4ad2-4d1f-92f0-d378d4d03b8e | BirdcageCoinNum5 | Int32 | 0 |  |  |  |
| Unknown | 4e861a9a-6b7f-4ac9-ba3e-6c7c5413acb2 | BirdcageItemGet5 | Boolean | False |  |  |  |
| Unknown | 1a67b605-57ef-4294-a6c6-117a5ff66de1 | BirdcageCoinNum6 | Int32 | 0 |  |  |  |
| Unknown | 072ae895-cab0-444b-9755-eaea232578d7 | BirdcageItemGet6 | Boolean | False |  |  |  |

### ItemGot_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | b5481247-c91f-4041-be1d-bdfc5a9cc34f | Herb | Boolean | False |  |  |  |
| Unknown | 7e07cd85-dca0-4dcb-b888-9893bb9420cb | ChemicalS | Boolean | False |  |  |  |
| Unknown | 3dcd092f-26e9-41cd-8f4a-64803b5734ec | Gunpowder | Boolean | False |  |  |  |
| Unknown | cb5e2840-23e3-49e7-81e9-91d95370753f | Alcohol | Boolean | False |  |  |  |
| Unknown | a3253e6a-a4ca-45ed-aeae-880367e42666 | Flower | Boolean | False |  |  |  |
| Unknown | 2e59214a-a3e4-4047-86dc-509b318e2f4b | ChemicalM | Boolean | False |  |  |  |
| Unknown | d1cb1535-a8c1-41aa-855a-1ac12a7463dc | ChemicalL | Boolean | False |  |  |  |
| Unknown | 6784217b-c4ec-4101-9d5d-d3adda30c0b1 | AcidBulletS | Boolean | False |  |  |  |

### StoreEpisode_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | de59c3ea-41cb-4dcc-91ac-c1a97f63dcb6 | DlcEpisode1_Got | Boolean | False |  |  |  |
| Unknown | 3217dfea-fea6-465d-8ad8-9c08234402d6 | DlcEpisode2_Got | Boolean | False |  |  |  |
| Unknown | 3c705969-31a7-44cf-969f-efb55da456e3 | DlcEpisode3_Got | Boolean | False |  |  |  |
| Unknown | 91ea82c6-3722-4b8f-b2fa-c3eacb4f9dd4 | DlcEpisode4_Got | Boolean | False |  |  |  |
| Unknown | ffd3b03f-d54a-4372-a5d7-cfd5504ff0ae | DlcEpisode5_Got | Boolean | False |  |  |  |
| Unknown | 2610a2fc-a188-40b8-a344-7b35b142f8d7 | DlcEpisode6_Got | Boolean | False |  |  |  |
| Unknown | feaaba20-6502-4f13-bfd8-f3fc5bc07a81 | DlcEpisode7_Got | Boolean | False |  |  |  |
| Unknown | 9efe1c2c-47bb-42ac-a2ab-6ee4905a0e27 | DlcEpisode8_Got | Boolean | False |  |  |  |

### StoreEpisodeSale_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 98689a80-0eda-48e2-8c65-e15ac22ac241 | DlcEpisode1_Sale | Boolean | False |  |  |  |
| Unknown | 9b13ca17-0f79-4cb7-8fc0-bdcf01649c49 | DlcEpisode1_SaleView | Boolean | False |  |  |  |
| Unknown | c4a6a017-8a73-4b7d-86c6-15dcfebe9790 | DlcEpisode1_SaleNever | Boolean | False |  |  |  |
| Unknown | 3d40e5dd-5c50-431e-8952-7be52f62e5f5 | DlcEpisode2_Sale | Boolean | False |  |  |  |
| Unknown | d9095221-09ed-4124-a625-64c7859803bd | DlcEpisode2_SaleView | Boolean | False |  |  |  |
| Unknown | c95b04ce-1250-4b3b-ad1b-9acbae51926e | DlcEpisode2_SaleNever | Boolean | False |  |  |  |
| Unknown | e4bb51b3-6696-4a56-9463-3d4fbff908b5 | DlcEpisode3_Sale | Boolean | False |  |  |  |
| Unknown | 1b4bcb37-b1c7-440f-b40c-2a744de7448f | DlcEpisode3_SaleView | Boolean | False |  |  |  |
| Unknown | 925f79d8-d4ff-4cfd-a002-7950843ac921 | DlcEpisode3_SaleNever | Boolean | False |  |  |  |
| Unknown | 1fcea8e6-f2f8-4435-accc-be8237647c65 | DlcEpisode4_Sale | Boolean | False |  |  |  |
| Unknown | ba355404-5406-463b-b1a7-1cfb5f4eaa5d | DlcEpisode4_SaleView | Boolean | False |  |  |  |
| Unknown | c72263fb-7c15-4e0e-bd85-2396f7452235 | DlcEpisode4_SaleNever | Boolean | False |  |  |  |
| Unknown | d1a5a456-cb78-45a9-9db5-5616646b98a8 | DlcEpisode5_Sale | Boolean | False |  |  |  |
| Unknown | 2c11fdd6-eb08-42e6-b984-9078295bc60c | DlcEpisode5_SaleView | Boolean | False |  |  |  |
| Unknown | 756649bf-8eaf-4f9b-993d-07f92fd737db | DlcEpisode5_SaleNever | Boolean | False |  |  |  |
| Unknown | cf8d935e-6f2f-4a87-824a-69e2c64f807e | DlcEpisode6_Sale | Boolean | False |  |  |  |
| Unknown | 0518b1f9-f3cd-4268-9141-1aeb0fcc0330 | DlcEpisode6_SaleView | Boolean | False |  |  |  |
| Unknown | 085294ad-647f-4aef-803a-2ac192993052 | DlcEpisode6_SaleNever | Boolean | False |  |  |  |
| Unknown | e1721661-0215-446c-9de9-8b11557fc9d9 | DlcEpisode7_Sale | Boolean | False |  |  |  |
| Unknown | bd874254-2e92-416a-8b2e-602e0f49f2ee | DlcEpisode7_SaleView | Boolean | False |  |  |  |
| Unknown | 76c669dc-bbf8-4d51-a3e1-9945e7c63f1c | DlcEpisode7_SaleNever | Boolean | False |  |  |  |

### c07_2_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 4f06740b-695a-4a36-b755-26cfcbea961a | c07_2_Main_StartSetupOK | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for start setup ready. | Written by `0_StartSetupOK` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_2_Main_StartSetupOK true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`); Gate package `c07_2_main_100.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_2_Main_StartSetupOK (`leveldesign/fsm/chapter7/chapter7_2/objectivefsm_c07_2.scn.20`) |
| Likely | 2f092cfc-802f-4767-a0bc-c813cfddea1c | c07_2_Main_ElectShockSet | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for elect shock set. | Written by `100_ElectShockSet` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_2_Main_ElectShockSet true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`); Gate package `c07_2_main_200.user.2` |
| Likely | 9bbeaa45-4c9f-415b-b530-457d0dfbc586 | c07_2_Main_NokogiriSet | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for saw set. | Written by `200_NokogiriSet` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets c07_2_Main_NokogiriSet true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`); Gate package `c07_2_main_300.user.2` |
| Likely | 02d82168-1e53-47b1-8e8b-babceae9526d | c07_2_Main_GameEnd | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for game end. | Required by game-flow package `c07_2_main_end.user.2`. | Gate package `c07_2_main_end.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_2_Main_GameEnd (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |

### c07_2_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | cd18a920-aa6b-4bf8-8b23-0b8070597057 | PL_IsFirst | Boolean | True | 21 DLC card-game flow: tracks 21 card-game state for player is first. | Written by `FadeReset` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets PL_IsFirst false (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=PL_IsFirst (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | dea8ed7a-c437-4f88-901c-b6b1884c7008 | IsFingerBetGame | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is finger bet game. | Read by `BKSoliloquy` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsFingerBetGame (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 100d18cf-5c3e-4517-8bfe-bea142539c66 | IsElectricBetGame | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is electric bet game. | Read by `BKSoliloquy` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsElectricBetGame (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 85f2ee58-e594-42a2-aad0-906fb61c69ea | IsSawBetGame | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is saw bet game. | Read by `BKSoliloquy` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsSawBetGame (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | cf7c31ad-b853-4684-858b-e5688cc55767 | IsTutorialEnd | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is tutorial end. | Written by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsTutorialEnd true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsTutorialEnd (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 0cecd09b-4bc5-4362-95d5-2a00c73871d9 | IsAlreadyTalking | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is already talking. | Written by `BKSoliloquy` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsAlreadyTalking true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsAlreadyTalking (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 11377d01-b50b-46bd-a374-dc7b18eec75b | IsFirst_DrawGame | Boolean | True | 21 DLC card-game flow: tracks 21 card-game state for is first draw game. | Written by `DrawTorture` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsFirst_DrawGame false (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsFirst_DrawGame (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 92f76093-e1a7-456e-8b3c-2b94bbd190ab | PL_Elec_Count | Int32 | 0 | 21 DLC card-game flow: counts player electric count. | Written by `PLTorture` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetInt`: sets PL_Elec_Count to 2 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | b61599e4-b90f-4d97-9b8a-a5826c0b54e9 | PL_Saw_First | Boolean | True | 21 DLC card-game flow: tracks 21 card-game state for player saw first. | Written by `PLTorture` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets PL_Saw_First false (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=PL_Saw_First (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | ae2f0d05-1709-42f4-a33a-e5b1591a90ee | BK_Elec_Count | Int32 | 0 | 21 DLC card-game flow: counts blackjack opponent electric count. | Written by `BKTorture` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetInt`: sets BK_Elec_Count to 1 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 52b921f6-81b0-473c-8551-84b8655ef89e | IsCERO_D | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is CERO d. | Read by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsCERO_D (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | a43863a3-21c3-433e-9f09-c6c6739e8e12 | CMD_InventoryOpen | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for command inventory open. | Read by `PLDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=CMD_InventoryOpen (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | ee837179-f7d7-45a2-9220-5652b2aaeeb4 | CMD_TakeCard | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for command take card. | Read by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=CMD_TakeCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 10184221-0eeb-4c1d-80fd-518507c566fb | CMD_NoTakeCard | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for command no take card. | Read by `PLDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=CMD_NoTakeCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | cbd6f175-8a02-4077-867a-0859cf1db0d8 | CMD_TableItemUIOpen | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for command table item uiopen. | Read by `PLDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=CMD_TableItemUIOpen (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 28dc9c76-d127-4980-ae26-9dcae57442b2 | PL_BanTakeCard | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for player ban take card. | Written by `300_LastBattle` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets PL_BanTakeCard true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=PL_BanTakeCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 574b77f7-c267-487e-9ec9-350eb1692af6 | IsFirstBetted | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is first betted. | Written by `200_IntroEvent` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsTortured true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`) |
| Likely | ab53493a-2458-402a-a9d9-8b58560c24d2 | IsUsedRemove | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is used remove. | Written by `BK_ItemUseCheck` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsUsedRemove true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsUsedRemove (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | c3f9a026-971c-4941-9a6f-3d2e0a4a6da8 | IsUsedBetUp2 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is used bet up 2. | Written by `BK_ItemUseCheck` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsUsedBetUp2 true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsUsedBetUp2 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 836b5379-266a-49d2-b222-ec7630f74ccd | IsBKSilence | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is bksilence. | Written by `BKSoliloquy` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsBKSilence true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsBKSilence (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | d9cb9761-a963-43ab-95c8-d90c9525f752 | IsFirstGetSPCard | Boolean | True | 21 DLC card-game flow: tracks 21 card-game state for is first get spcard. | Written by `PLDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsFirstGetSPCard false (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsFirstGetSPCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 40f43029-41e1-481f-94e3-fbd651b82a76 | IsStoryLastBattle | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is story last battle. | Written by `300_LastBattle` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsStoryLastBattle true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsStoryLastBattle (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 865460c5-fbfa-419a-b71c-df90feaaa24e | IsBKPowerType | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is bkpower type. | Read by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsBKPowerType (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | f98533f5-99c5-4fcc-b672-ddfee3840110 | IsSPCardOpen | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is spcard open. | Written by `0_RestartSetting` in `levelfsm_c07_2_storymode.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsSPCardOpen true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsSPCardOpen (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 31935df5-206d-4bb5-8edf-5114c1b28baa | ItemDrawPercent | Int32 | 0 | 21 DLC card-game flow: stores the percentage/weight value for item draw percent. | Written by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Write via `app.fsm.RandomFlagSet`: sets random int 0..4 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 486ead57-f9a0-47df-a15f-70d1b595810b | IsPLGotSPCard | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is plgot spcard. | Written by `FadeReset` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsPLGotSPCard false (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsPLGotSPCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 0cbf1a53-4e44-4984-9eb4-ba6582cb328d | IsBKGotSPCard | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is bkgot spcard. | Written by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsBKGotSPCard true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsBKGotSPCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | fff9fcd1-2462-4240-9cd5-bee773f8a7db | IsCardGameStoryMode | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is card game story mode. | Read by `BKSoliloquy` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsCardGameStoryMode (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 028aa07f-47d2-42f8-a6dd-fad2096ef53d | IsCardGameSurvivalMode | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is card game survival mode. | Read by `BK_ItemUseCheck` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsCardGameSurvivalMode (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 77d04cae-fe73-4a5f-9ad7-f6e7a3d2188e | IsCardGameSurvivalPlusMode | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is card game survival plus mode. | Read by `BGMSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=IsCardGameSurvivalPlusMode (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Unknown | aafe5677-1400-4405-9bdc-d58d105c5f08 | PL_BanUseSPCard | Boolean | False |  |  |  |
| Likely | c636610f-5358-442c-ab9d-64e0b98c0d61 | BK_BanUseSPCard | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent ban use spcard. | Read by `BK_ItemUseCheck` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=BK_BanUseSPCard (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 9f2faec3-d63a-451b-90e3-4f59e6969a53 | PL_UseReplace | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for player use replace. | Read by `CardItems_Action` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=PL_UseReplace (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Unknown | 6b3c298b-a252-4d06-b9ee-ede4f0a018a6 | BK_UseReplace | Boolean | False |  |  |  |
| Likely | 3429cac2-d8f4-47dc-85c6-40e3ddede238 | BK_UseEscape | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent use escape. | Read by `Survival_BKTorture` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=BK_UseEscape (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | a41898da-4486-452c-bade-4562ce003887 | BK_UseForceRoundEnd | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent use force round end. | Read by `HandLight` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_UseForceRoundEnd (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | f945da91-7bd2-456b-ba76-fe98fb050482 | BK_Em9800 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9800. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9800 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 3269e522-1b4e-40f5-b74a-adccaf7b2feb | BK_Em9801 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9801. | Read by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9801 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | b74ef854-7fa4-416a-bcfa-00c8a73c47bc | BK_Em9802 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9802. | Read by `BKDrawPhase_Test` in `levelfsm_c07_2_common.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9802 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | 47549762-11fb-42e2-841e-791de6a07c0e | BK_Em9803 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9803. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9803 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 6b1926b5-b949-4d47-bd0e-ebcd107a99db | BK_Em9804 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9804. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9804 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 01ab0987-85ef-40bc-b118-331f2f0d1ec2 | BK_Em9805 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9805. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9805 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | e75bcdb4-d7b9-4a3b-9015-2f86e93d08f4 | BK_Em9806 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9806. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9806 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | b3685bb1-11d4-4d4c-a508-86f9e9bdca9c | BK_Em9807 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9807. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9807 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 24ed6744-e8b9-4cd3-86c5-ff1708e098da | BK_Em9808 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9808. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9808 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | bd58bdc8-0e30-431a-9cfb-376100faf8b3 | BK_Em9809 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for blackjack opponent enemy 9809. | Read by `BKSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=BK_Em9809 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | b7e7767e-9ec2-4058-96f6-93f5ede8d385 | RareEncountFlag | Int32 | 0 | 21 DLC card-game flow: counts rare encount flag. | Written by `Survival_p` in `levelfsm_c07_2_survivalmode.scn.20`. | Write via `app.fsm.RandomFlagSet`: sets random int 0..5 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | f48a307f-c506-4021-b7c1-07f0f17b4236 | RareEncountThisGame | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for rare encount this game. | Written by `Survival_p` in `levelfsm_c07_2_survivalmode.scn.20`. | Write via `via.fsm.action.SetBool`: sets RareEncountThisGame true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=RareEncountThisGame (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | c5c4e8e1-cab4-462c-b50c-e321f3b7e0fe | RewardSPCard1 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for reward spcard 1. | Read by `PLSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=RewardSPCard1 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 30badc70-b033-479f-b5ef-404cfee41f50 | RewardSPCard2 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for reward spcard 2. | Read by `PLSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=RewardSPCard2 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 2aa77a42-14a5-4b06-b87f-6a0dfee391c0 | RewardSPCard3 | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for reward spcard 3. | Read by `PLSPCardFirstSet` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=RewardSPCard3 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | cd3a2b38-a1a0-4f60-913e-03b4358a89b6 | RewardEveryware | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for reward everyware. | Read by `GoldenEvery` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=RewardEveryware (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | ae25362d-76d1-47d2-bfa7-618dddc2cb36 | RandomCardInit | Int32 | 0 | 21 DLC card-game flow: stores the random selection value for random card init. | Written by `100_CardInitSet` in `levelfsm_c07_2_storymode.scn.20`. | Write via `app.fsm.RandomFlagSet`: sets random int 0..2 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`) |
| Likely | 43e28282-51b5-4f4f-bfc8-599654f52265 | RandomHoffmanActions | Int32 | 0 | 21 DLC card-game flow: stores the random selection value for random hoffman actions. | Written by `100_HoffmanActions` in `levelfsm_c07_2_storymode.scn.20`. | Write via `app.fsm.RandomFlagSet`: sets random int 0..2 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_storymode.scn.20`) |
| Likely | 1853e3b3-45ce-482d-aa9d-89d7469863fb | IsTrickyBluff | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is tricky bluff. | Written by `FadeReset` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsTrickyBluff false (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsTrickyBluff (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |
| Likely | ec915e81-17e1-4891-87b1-540a201f7085 | IsWeekType | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is week type. | Written by `Survival` in `levelfsm_c07_2_survivalmode.scn.20`. | Write via `via.fsm.action.SetBool`: sets IsWeekType true (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsWeekType (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | 8e723b6a-6fdc-4e79-8f6d-df690f927112 | IsSKUKorea | Boolean | False | 21 DLC card-game flow: tracks 21 card-game state for is skukorea. | Read by `CardGameMaster` in `levelfsm_c07_2_survivalmode.scn.20`. | Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=IsSKUKorea (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_survivalmode.scn.20`) |
| Likely | fbdb8941-6bd7-4b6f-83a8-7ad34ab49070 | ItemUsedCount | Int32 | 0 | 21 DLC card-game flow: counts item used count. | Written by `FadeReset` in `levelfsm_c07_2_common.scn.20`. | Write via `via.fsm.action.SetInt`: sets ItemUsedCount to 0 (`leveldesign/fsm/chapter7/chapter7_2/levelfsm_c07_2_common.scn.20`) |

### c07_3_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | 20a2b881-2374-4ae5-a12c-e7cbe1cc610c | c07_3_Main_FirstTutorialPhaseEnd | Boolean | False | Nightmare DLC wave flow: marks that first tutorial phase has ended/cleared. | Required by game-flow package `c07_3_main_100.user.2`. | Gate package `c07_3_main_100.user.2` |
| Likely | 7ff897fd-30ea-490c-b683-2b5a7f4935ed | c07_3_Main_Wave1-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 1 1. | Required by game-flow package `c07_3_main_200.user.2`. | Gate package `c07_3_main_200.user.2` |
| Likely | 19fe0900-0032-4dd8-94cf-0ce8fbad7281 | c07_3_Main_Wave1-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 1 2. | Required by game-flow package `c07_3_main_250.user.2`. | Gate package `c07_3_main_250.user.2` |
| Likely | d9ed71e5-d264-4fcc-a5a6-cdfc2bd7b739 | c07_3_Main_Wave2-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 2 1. | Required by game-flow package `c07_3_main_300.user.2`. | Gate package `c07_3_main_300.user.2` |
| Likely | 375a215d-17e1-4715-b46d-382200c4fa2f | c07_3_Main_Wave2-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 2 2. | Required by game-flow package `c07_3_main_350.user.2`. | Gate package `c07_3_main_350.user.2` |
| Likely | 1e8ead43-b091-4b51-a3c7-e373e76e880e | c07_3_Main_Wave3-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 3 1. | Required by game-flow package `c07_3_main_400.user.2`. | Gate package `c07_3_main_400.user.2` |
| Likely | 54130fd3-5d3c-433b-a1d5-2c670efba0df | c07_3_Main_Wave3-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 3 2. | Required by game-flow package `c07_3_main_450.user.2`. | Gate package `c07_3_main_450.user.2` |
| Likely | 56253996-76ac-4e77-a0bf-1254ae438215 | c07_3_Main_Wave4-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 4 1. | Required by game-flow package `c07_3_main_500.user.2`. | Gate package `c07_3_main_500.user.2` |
| Likely | 49cc70c6-87fb-4c77-aeb1-a01d6971a725 | c07_3_Main_Wave4-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 4 2. | Required by game-flow package `c07_3_main_550.user.2`. | Gate package `c07_3_main_550.user.2` |
| Likely | bea716d8-05dc-4e7b-939f-9c356b08f23f | c07_3_Main_Wave5-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 5 1. | Required by game-flow package `c07_3_main_600.user.2`. | Gate package `c07_3_main_600.user.2` |
| Likely | bb111d8b-2545-4510-95e0-1f5e4e4b6144 | c07_3_Main_Wave5-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 5 2. | Required by game-flow package `c07_3_main_650.user.2`. | Gate package `c07_3_main_650.user.2` |
| Likely | d9b98d2b-d313-4023-b933-31ef3cec16d1 | c07_3_Main_Last | Boolean | False | Nightmare DLC wave flow: tracks last. | Required by game-flow package `c07_3_main_700.user.2`. | Gate package `c07_3_main_700.user.2` |
| Likely | cf4d2d35-fc0c-4b73-8e02-2ffc5cdbf24f | c07_3_OPEventEnd | Boolean | False | Nightmare DLC wave flow: marks that opevent has ended/cleared. | Required by game-flow package `c07_3_main_0.user.2`. | Gate package `c07_3_main_0.user.2` |
| Likely | b63b6b49-3639-43e1-8708-4e8a662fa59a | c07_3_Main_Wave6-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 6 1. | Required by game-flow package `c07_3_main_700_hard.user.2`. | Gate package `c07_3_main_700_hard.user.2` |
| Likely | 79f68f20-32d7-493e-a1d3-089814634e28 | c07_3_Main_Wave6-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 6 2. | Required by game-flow package `c07_3_main_750.user.2`. | Gate package `c07_3_main_750.user.2` |
| Likely | 01330db2-3a61-41ba-9b8d-92eae8c868f9 | c07_3_Main_Wave7-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 7 1. | Required by game-flow package `c07_3_main_800.user.2`. | Gate package `c07_3_main_800.user.2` |
| Likely | ff2627f3-9d72-4145-a718-fcb9dd9acdcd | c07_3_Main_Wave7-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 7 2. | Required by game-flow package `c07_3_main_850.user.2`. | Gate package `c07_3_main_850.user.2` |
| Likely | 884b0054-6abb-4fe8-ad8c-d770bb01d701 | c07_3_Main_Wave8-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 8 1. | Required by game-flow package `c07_3_main_900.user.2`. | Gate package `c07_3_main_900.user.2` |
| Likely | 41c7b056-0d9f-4cb2-ad7e-c55ca2ae8e7b | c07_3_Main_Wave8-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 8 2. | Required by game-flow package `c07_3_main_950.user.2`. | Gate package `c07_3_main_950.user.2` |
| Likely | 407bb155-6933-4912-91fd-e244fc13f82f | c07_3_Main_Wave9-1End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 9 1. | Required by game-flow package `c07_3_main_1000.user.2`. | Gate package `c07_3_main_1000.user.2` |
| Likely | 934051d3-d685-47c1-a5ea-dc43f6466a71 | c07_3_Main_Wave9-2End | Boolean | False | Nightmare DLC wave flow: marks completion of wave 9 2. | Required by game-flow package `c07_3_main_1050.user.2`. | Gate package `c07_3_main_1050.user.2` |

### VRTutorial_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 5f4c7dd4-7cda-4f39-b574-1a76c25ba88d | VRTutorial_isStateEnd | Boolean | False |  |  |  |
| Unknown | 3a4573bb-f599-427a-8820-feca813e5cd8 | VRTutorial_isDebugSuccess | Boolean | False |  |  |  |
| Unknown | c4d4ea1d-49c6-491f-837e-726b28298061 | VRTutorial_isMessageWait | Boolean | False |  |  |  |

### UnLockAchievement_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | a5f6f60c-0a9d-4161-b0db-75eedd02524c | DetailSerachAndItemGet | Boolean | False | Achievement unlock tracker: marks that the player obtained detail search and item. | Written by `50_UnlockAchievement_DetailSerachAndItemGet` in `levelfsm_c01.scn.20`. | Write via `via.fsm.action.SetBool`: sets DetailSerachAndItemGet true (`leveldesign/fsm/chapter1/levelfsm_c01.scn.20`); Read via `via.fsm.ConditionUserDataExpression`: requires false; ExpressionReferenceType=1; name=DetailSerachAndItemGet (`leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20`) |

### c07_4_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Likely | b43367d0-71e1-4d05-b81d-50e95d1a8e65 | c07_4_Main_OPEventEnd | Boolean | False | Daughters DLC flow: marks that opevent has ended/cleared. | Required by game-flow package `c07_4_main_100.user.2`. | Gate package `c07_4_main_100.user.2` |
| Likely | 58d933bb-13fd-4231-8c7b-ad0629b0bee1 | c07_4_Main_GetClothes | Boolean | False | Daughters DLC flow: tracks get clothes. | Required by game-flow package `c07_4_main_200.user.2`. | Gate package `c07_4_main_200.user.2` |
| Likely | eafaa2f6-77b3-4610-bac2-6f1b5254000a | c07_4_Main_ChildRoomEvent1End | Boolean | False | Daughters DLC flow: marks that child room event 1 has ended/cleared. | Required by game-flow package `c07_4_main_300.user.2`. | Gate package `c07_4_main_300.user.2` |
| Likely | 650e5ebc-6ff0-44f3-a5d4-91c6e8a4fc51 | c07_4_Main_ChildRoomEvent2End | Boolean | False | Daughters DLC flow: marks that child room event 2 has ended/cleared. | Required by game-flow package `c07_4_main_350.user.2`. | Gate package `c07_4_main_350.user.2` |
| Likely | d5cc0e23-a219-42dd-a54e-b6f116024b8d | c07_4_Main_MargaretEventEnd | Boolean | False | Daughters DLC flow: marks that Marguerite event has ended/cleared. | Required by game-flow package `c07_4_main_500.user.2`. | Gate package `c07_4_main_500.user.2` |
| Likely | 5a57031a-f173-45bf-ac28-4f4fd35a5eb7 | c07_4_Main_GetRope | Boolean | False | Daughters DLC flow: tracks get rope. | Required by game-flow package `c07_4_main_600.user.2`. | Gate package `c07_4_main_600.user.2` |
| Likely | d9bcbb97-07a8-41b6-a0b3-dba4c2c942c9 | c07_4_Main_JackEventEnd | Boolean | False | Daughters DLC flow: marks that Jack event has ended/cleared. | Required by game-flow package `c07_4_main_620.user.2`. | Gate package `c07_4_main_620.user.2` |
| Likely | 1242f645-c329-4b68-9c6a-903b818c5327 | c07_4_Main_KeyGetEventEnd | Boolean | False | Daughters DLC flow: marks that the player obtained key get event. | Required by game-flow package `c07_4_main_800.user.2`. | Gate package `c07_4_main_800.user.2` |
| Likely | b722a1d5-5a39-41b9-84a4-8b6e83693f2a | c07_4_Main_EndingEventEnd | Boolean | False | Daughters DLC flow: marks that ending event has ended/cleared. | Required by game-flow package `c07_4_main_900.user.2`. | Gate package `c07_4_main_900.user.2` |
| Likely | 5b10b1bb-7734-4a71-a1d7-d66ab17cdeb9 | c07_4_Main_LucasEvent | Boolean | False | Daughters DLC flow: tracks Lucas event. | Required by game-flow package `c07_4_main_400.user.2`. | Gate package `c07_4_main_400.user.2` |
| Likely | 7b3cf972-eba6-41dc-b7ae-01f240b5bccd | c07_4_Main_UseRope | Boolean | False | Daughters DLC flow: tracks use rope. | Required by game-flow package `c07_4_main_640.user.2`. | Gate package `c07_4_main_640.user.2` |
| Likely | 77b498ed-018c-405c-837c-d1fa23c91979 | c07_4_Main_EscapePlayRoom | Boolean | False | Daughters DLC flow: tracks escape play room. | Required by game-flow package `c07_4_main_660.user.2`. | Gate package `c07_4_main_660.user.2` |
| Likely | 7966b7a8-7276-4fe7-8687-6b78f75407be | c07_4_Main_EnterMainHole | Boolean | False | Daughters DLC flow: marks that the player entered main hole. | Required by game-flow package `c07_4_main_700.user.2`. | Gate package `c07_4_main_700.user.2` |
| Likely | 4769cc43-0c0c-4f7e-813d-a10ac56998f6 | c07_4_Main_EnterUnderFloor | Boolean | False | Daughters DLC flow: marks that the player entered under floor. | Required by game-flow package `c07_4_main_850.user.2`. | Gate package `c07_4_main_850.user.2`; Read via `via.fsm.ConditionUserDataExpression`: requires true; ExpressionReferenceType=1; name=c07_4_Main_EnterUnderFloor (`leveldesign/fsm/chapter7/chapter7_4/marguerite_7_4.scn.20`) |

### c07_4_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | a0cbe603-4c46-484d-93fc-3b1d08750304 | c07_4_LightDown | Boolean | False |  |  |  |
| Unknown | 0d3551a9-6b8a-4637-977c-ffac3b25705e | c07_4_GameOverEvent | Boolean | False |  |  |  |
| Unknown | 77588a40-5e36-447f-bae6-9b1302139059 | c07_4_RandomKnock | Int32 | 0 |  |  |  |
| Unknown | 7c977c3c-02e1-48a3-9c00-1ce1162d39ca | c07_4_JackTranceform_MegLoopStart | Boolean | False |  |  |  |
| Unknown | 3531e7bf-9252-4003-8417-9856308b0edc | c07_4_LucasPassClear | Boolean | False |  |  |  |
| Unknown | 49b296d8-6d20-4dd5-acce-77d1477a894d | c07_4_SmartPhoneControll | Int32 | 0 |  |  |  |
| Unknown | f9173784-eb5f-4c1c-a134-f170a4d680de | c07_4_LighterLighted | Boolean | False |  |  |  |
| Unknown | a37816da-ade7-46ce-bf16-8ce2d0222e45 | c07_4_LighterOpened | Boolean | False |  |  |  |
| Unknown | 4f847c31-cf35-4844-ad67-d1fff2944324 | c07_4_IsMessageOut | Boolean | False |  |  |  |
| Unknown | 340cc3c2-70f7-4036-bd9e-b151878241c7 | c07_4_TrueEndClear | Boolean | False |  |  |  |
| Unknown | f84974a4-50e1-4bdd-a3d7-053d24c96b77 | c07_4_BadEndClear | Boolean | False |  |  |  |
| Unknown | 6cc054d9-e437-46e6-8cbf-6a42c953f57f | c07_4_FirstInteract_PC | Boolean | False |  |  |  |
| Unknown | cb38c842-55de-4bc1-b720-9b471e09cc63 | c07_4_FirstInteract_Keypic | Boolean | False |  |  |  |
| Unknown | f9d2044b-be8f-4d75-9fdc-75bf91e6c09d | c07_4_FirstInteract_Trophy | Boolean | False |  |  |  |
| Unknown | 06339ad9-2d99-4b4d-8126-a1458c2d9d5b | c07_4_IntroJackExit | Boolean | False |  |  |  |
| Unknown | 782e0f51-5eae-4500-9ce7-fa27dc057acc | c07_4_LucasEventCloneEnd | Boolean | False |  |  |  |
| Unknown | 2d766cad-45db-42d0-8ef5-559a9846412d | c07_4_Interact_MiaMemo02 | Boolean | False |  |  |  |
| Unknown | 0ea5acfb-eeaa-4b78-8547-fa73a605f6d8 | c07_4_Interact_MedicineMaterialB | Boolean | False |  |  |  |
| Unknown | 534a8666-5808-4ffb-a42e-e7d6e61ea42c | c07_4_PLBreathFear | Boolean | False |  |  |  |
| Unknown | ba740ff4-062e-4e6a-b9c2-e406590f0603 | c07_4_UseKeypicBathroom | Boolean | False |  |  |  |
| Unknown | b54fe3aa-a8a8-4923-a9e8-5e0bbc992260 | c07_4_InteractHalfShutter | Boolean | False |  |  |  |
| Unknown | 06a9abe7-417e-4c0d-bf58-46e9e3529940 | c07_4_DragLucasStart | Boolean | False |  |  |  |
| Unknown | 1834e5ef-3476-455a-8412-a05e387a2281 | c07_4_MegKeyEvent_Start | Boolean | False |  |  |  |
| Unknown | 1bf35556-5f6c-4bc7-a430-72952a8be31b | c07_4_JackBreakPlayroomDoor | Boolean | False |  |  |  |

### AdditionalItemGetCutin

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 53500db0-3c6e-418b-bd36-1e9979d4dd79 | StorePrivilege_A | Boolean | False |  |  |  |
| Unknown | e55fb5db-3aee-4329-adc2-fc8bb0bd4784 | StorePrivilege_B | Boolean | False |  |  |  |
| Unknown | 44694ee4-3ce7-406b-b979-fafd24cb9b4c | StorePrivilege_C | Boolean | False |  |  |  |
| Unknown | b6055616-6f76-4c38-80d8-eb6002c07aa7 | StorePrivilege_D | Boolean | False |  |  |  |
| Unknown | fd0225f5-c8ee-4415-ac96-78922e333d76 | StorePrivilege_E | Boolean | False |  |  |  |
| Unknown | 047c3549-523c-4896-9688-441b84852aa5 | StorePrivilege_G | Boolean | False |  |  |  |
| Unknown | 9888fc71-bea0-4c54-8c87-754e81da6a81 | DLC_Item_A | Boolean | False |  |  |  |
| Unknown | 7e464ffa-4eef-4978-858b-86c61a3b36b0 | DLC_Item_B | Boolean | False |  |  |  |
| Unknown | cb6f76c6-24ab-407c-8bdd-73d72bfc875b | DLC_Item_C | Boolean | False |  |  |  |
| Unknown | f908aa7f-7fd4-40e5-bed0-842c4d059481 | DLC_Item_D | Boolean | False |  |  |  |
| Unknown | a074a192-9213-4a13-aa54-608468a5376b | DLC_Item_E | Boolean | False |  |  |  |

### AdditionalItemUnlockCutin

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 11f7403b-886d-439d-b0e5-989785c9dc85 | StorePrivilege_A | Boolean | False |  |  |  |
| Unknown | 7fc5e2ad-2150-4d32-b056-f6281f5d109f | StorePrivilege_B | Boolean | False |  |  |  |
| Unknown | 370f0fe9-e433-4681-a9d2-3265120c1cb2 | StorePrivilege_C | Boolean | False |  |  |  |
| Unknown | 33d5acbe-f806-435a-8e7a-3e92515aef5b | StorePrivilege_D | Boolean | False |  |  |  |
| Unknown | d66a2f9d-9132-4d9f-8b53-73a4f747ba64 | StorePrivilege_E | Boolean | False |  |  |  |
| Unknown | df7b3299-5f18-4943-8275-dafbed915855 | StorePrivilege_G | Boolean | False |  |  |  |
| Unknown | f26224cd-f500-4370-94a9-6789617ef4b9 | DLC_Item_A | Boolean | False |  |  |  |
| Unknown | da146919-b9c1-4038-b61d-bf0ec100f07f | DLC_Item_B | Boolean | False |  |  |  |
| Unknown | 9670eb26-a852-4606-9238-bcaa71381d82 | DLC_Item_C | Boolean | False |  |  |  |
| Unknown | e52cf51b-3587-4666-a29e-14741850df11 | DLC_Item_D | Boolean | False |  |  |  |
| Unknown | e1d348d8-69da-4101-8fd8-7e98c26abe3a | DLC_Item_E | Boolean | False |  |  |  |

### Ambassador_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | be5967a7-0986-4748-82ea-ea32fa64b791 | c01_KeyPickDrawerOpenInFF | Boolean | False |  |  |  |

### ProgressiveNumber_c01_Trial

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 559a9a92-0a02-4420-a0c9-b5c4d59c274b | c01_Main_A | Int32 | 0 |  |  |  |
| Unknown | a6878173-ba80-488d-9151-45f8911209df | c01_Main_B | Int32 | 0 |  |  |  |

### ProgressiveNumber_c07_2

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 23e04e6b-95e2-4fc7-a95f-59b97f0aecfc | c07_2_Main | Int32 | 0 |  |  |  |

### c01_Main_A_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 892b53fe-a5a6-471e-a22f-5976a230fcdb | c01_Main_A_FuseSet | Boolean | False |  |  |  |
| Unknown | 3e2adc10-8df3-402f-8451-ea3e7e952675 | c01_Main_A_BasementKeyGet | Boolean | False |  |  |  |
| Unknown | 7cd9cc64-9183-46d5-848a-f97c65abc4e1 | c01_Main_A_WalkIntoBasement | Boolean | False |  |  |  |
| Unknown | 44b72120-256e-427a-90e9-9a58275d38b1 | c01_Main_A_WentBackTo1F | Boolean | False |  |  |  |
| Unknown | f71ae15e-c6b0-4cfe-bf73-60aba9707590 | c01_Main_A_WentUpToTheAttic | Boolean | False |  |  |  |
| Unknown | a22c194c-4760-486d-abbd-6bd97c5d4e51 | c01_Main_A_WatchEnding | Boolean | False |  |  |  |

### c01_Main_B_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 1f2e0832-f9ac-477a-bd8c-bd897331e794 | c01_Main_B_UnWrittenNoteGet | Boolean | False |  |  |  |
| Unknown | 13c451cd-082f-4653-bb9e-e477e0f6963a | c01_Main_B_AchievePointCondition01 | Boolean | False |  |  |  |
| Unknown | 6faff30a-cdc0-4904-b62e-23afd201cef1 | c01_Main_B_ClearPointing01 | Boolean | False |  |  |  |
| Unknown | 3fec84d0-f0de-49ad-a39c-68f98ad6debe | c01_Main_B_AchievePointCondition02 | Boolean | False |  |  |  |
| Unknown | 86128004-567d-4117-b569-8ef788051eb8 | c01_Main_B_ClearPointing02 | Boolean | False |  |  |  |
| Unknown | c570723c-d67f-436f-aa17-57397c7948d4 | c01_Main_B_AchievePointCondition03 | Boolean | False |  |  |  |
| Unknown | 33e03d2b-d385-4455-82ff-497cec8f102f | c01_Main_B_ClearPointing03 | Boolean | False |  |  |  |
| Unknown | d26dc5fb-d819-4733-b804-255f7e907fc2 | c01_Main_B_AchievePointCondition04 | Boolean | False |  |  |  |
| Unknown | d936b4a7-18bb-4a29-a128-a72b665b8f19 | c01_Main_B_ClearPointing04 | Boolean | False |  |  |  |
| Unknown | e697b0cf-524b-4f58-b645-ab5914387866 | c01_Main_B_AchievePointCondition05 | Boolean | False |  |  |  |
| Unknown | 489fff6f-e1f3-4e11-a55b-d2e1bd7d17f2 | c01_Main_B_ClearPointing05 | Boolean | False |  |  |  |

### Ambassador_TU_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 2d92f7a8-94d0-4385-9d8f-388f14005a01 | c01_Mold_Exit | Uint32 | 0 |  |  |  |
| Unknown | a3c127e2-bd10-4745-818c-766a09b11f24 | c01_Mold_BlackOut | Boolean | False |  |  |  |
| Unknown | 18f829d4-ca83-4f86-8ffd-7eac57a9f2cb | c01_Mold_CheckExit | Boolean | False |  |  |  |
| Unknown | ae367d7d-3604-4f2c-9420-0df09efa03ad | c01_Mold_StartToExit | Boolean | False |  |  |  |
| Unknown | 4c555d23-24d0-4b4c-8728-051af1acd631 | AmbassadorTU_InteractAssetCheck | Boolean | False |  |  |  |
| Unknown | 4c80b955-ed5c-4c38-9ce0-0d1aaffaa798 | AmbassadorTU_ShotPainting | Boolean | False |  |  |  |
| Unknown | 3cf10e45-e9b9-4b6b-9ef4-de338b08599c | AmbassadorTU_WatchMirrorAndQuickTurn | Boolean | False |  |  |  |
| Unknown | 23d4c9bf-38af-402b-bddc-18fb916e2249 | AmbassadorTU_InfecttionEvent01Start | Boolean | False |  |  |  |
| Unknown | 96689e16-bbc3-4c98-acda-5f5900fa502a | AmbassadorTU_InfecttionEvent02Start | Boolean | False |  |  |  |
| Unknown | f1241f53-0188-4f2f-a864-296f7bc4706b | AmbassadorTU_WatchActressGhost1st | Boolean | False |  |  |  |
| Unknown | fedcec92-1b4f-4cc9-9eb5-6a5d0ff085e7 | AmbassadorTU_WatchActressGhost2nd | Boolean | False |  |  |  |
| Unknown | 5892f670-a34a-4a60-816a-d58290fc1202 | AmbassadorTU_WatchActressGhost3rd | Boolean | False |  |  |  |
| Unknown | a74497a5-207b-4bdc-b38f-b0684a35af61 | AmbassadorTU_HandGunBulletGet | Boolean | False |  |  |  |
| Unknown | e9557b54-b869-456e-8712-7a5d1bb9f7ac | AmbassadorTU_NoBulletPhoneInteract | Boolean | False |  |  |  |
| Unknown | 7e7dbd5a-560c-4239-bba4-1f0c6626a0d8 | AmbassadorTU_BlackOut_InfectionEnd | Boolean | False |  |  |  |

### IMD_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 7a6a9366-3f8d-4d09-b983-584684d1fa4d | Enemy_Random_Value | Int32 | 0 |  |  |  |
| Unknown | ae282e54-3ecb-4e04-bec9-e17a184b13ec | IMD_LongHandMotherAppearance | Boolean | False |  |  |  |

### c03_IMD_Main_Flag

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 487b484e-2e25-45f0-b402-7749757c9b25 | FatherIsDown | Boolean | False |  |  |  |
| Likely | 0d53ada7-b0d8-40ff-9b99-16e6868d0751 | MotherIsDown | Boolean | False | Ethan Must Die DLC flow: tracks Marguerite is down. | Required by game-flow package `c03_imd_main_end.user.2`. | Gate package `c03_imd_main_end.user.2` |
| Likely | d76219fa-1d95-4c60-b80e-3dde7db52991 | c03_IMD_Main_EnterTheHall | Boolean | False | Ethan Must Die DLC flow: marks that the player entered the hall. | Required by game-flow package `c03_imd_main_100.user.2`. | Gate package `c03_imd_main_100.user.2` |
| Likely | 60a2b59d-58a5-4b46-b150-2c2f519a4498 | c03_IMD_Main_EnterTheHall2F | Boolean | False | Ethan Must Die DLC flow: marks that the player entered the hall 2F. | Required by game-flow package `c03_imd_main_200.user.2`. | Gate package `c03_imd_main_200.user.2` |
| Likely | fae0c425-286a-44da-b6d0-5b7099dde4ed | c03_IMD_Main_GetDownTo1F | Boolean | False | Ethan Must Die DLC flow: tracks get down to 1F. | Required by game-flow package `c03_imd_main_300.user.2`. | Gate package `c03_imd_main_300.user.2` |
| Likely | 043c668e-b840-4c67-8778-ee571685d2fe | c03_IMD_Main_GHKeyGet | Boolean | False | Ethan Must Die DLC flow: marks that the player obtained ghkey. | Required by game-flow package `c03_imd_main_350.user.2`. | Gate package `c03_imd_main_350.user.2` |
| Likely | 53a555ed-d3c7-42a0-8e2b-c87b95cb4303 | c03_IMD_Main_GetBackToHall | Boolean | False | Ethan Must Die DLC flow: tracks get back to hall. | Required by game-flow package `c03_imd_main_400.user.2`. | Gate package `c03_imd_main_400.user.2` |
| Likely | 3c39325a-fe0e-4b3c-9aa6-4a9b8467a54a | c03_IMD_Main_EnterGreenHouse | Boolean | False | Ethan Must Die DLC flow: marks that the player entered green house. | Required by game-flow package `c03_imd_main_500.user.2`. | Gate package `c03_imd_main_500.user.2` |
| Likely | c5c9a7f9-bab1-444a-8f02-f23752a0e912 | c03_IMD_Main_EnterBattleAreaInGH | Boolean | False | Ethan Must Die DLC flow: marks that the player entered battle area in greenhouse. | Required by game-flow package `c03_imd_main_600.user.2`. | Gate package `c03_imd_main_600.user.2` |

### ProgressiveNumber_c03_IMD

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 0111d36d-b83c-4f56-b91c-f2947d232d67 | c03_IMD_Main | Int32 | 0 |  |  |  |

### ExtraContentUnlockCutin

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | ed5d7443-ceb4-4865-8208-0186fe12107a | Chapter7_1 | Boolean | False |  |  |  |
| Unknown | 3111fe99-b4fc-4a39-bf10-f7cb73d35242 | Chapter7_2 | Boolean | False |  |  |  |
| Unknown | 84cb41ad-3760-4cb9-a951-d903b66da7de | Chapter7_3 | Boolean | False |  |  |  |
| Unknown | 3f2bdd78-5a34-4f11-bcf9-d8f028d01a72 | Chapter7_4 | Boolean | False |  |  |  |
| Unknown | 3b165def-99b2-43c7-848d-afc04d235b9a | Chapter8 | Boolean | False |  |  |  |
| Unknown | 6671b4f1-c7ed-4939-a8c3-e45a1903a8a1 | Chapter9 | Boolean | False |  |  |  |

### ProgressiveNumber_c09

| Status | GUID | Variable | Type | Baseline | Meaning / effect | Observed trigger / context | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Unknown | 6a8aec5d-95e4-4add-81c1-5eeaf6e3ea0d | c09_1_Main | Int32 | 0 |  |  |  |
| Unknown | 7507b954-6667-49f9-b5db-2fe83a651a68 | c09_2_Main | Int32 | 0 |  |  |  |
| Unknown | 5def189a-f0d7-49e2-8eef-55684956cf52 | c09_3_Main | Int32 | 0 |  |  |  |
| Unknown | f3aa025d-7c06-4f5e-bb00-2e464c36290c | c09_4_Main | Int32 | 0 |  |  |  |

