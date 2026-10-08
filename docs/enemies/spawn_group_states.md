# SpawnGroup state reference

Use this reference to choose an actual RE7 FSM state without finding native objects yourself. The [complete CSV](spawn_group_states.csv) contains **127 phase conditions from 12 campaign controllers**, covering the guest house, main house, Old House, testing area, boathouse, ship, mines and finale. Open it in a spreadsheet or search it as text, then copy the **State** cell into the SpawnGroups sheet. Do not copy `ChapterSection` or `PhaseHint` into State.

Start with the [contributor guide](spawn_groups.md) for placement, position and delay rules. If the encounter should depend only on entering a room, leave State blank and use a position trigger. These phase conditions are for encounters that deliberately depend on story timing.

## What a phase condition means

The catalog's numeric names are literal FSM state names, not guessed chapter IDs or a comparison against a progression variable. For example:

```text
2efaa57c-6ae1-4810-aeb1-c0d3b1642fd8|0|200
```

This asks whether the `c03_1_MainFlow` object's root core is currently in its state named `200`. The scene's action for that state references the gate containing `c03_1_Main_GetFloorDoorKey`. Its useful interpretation is **the phase waiting for the Hatch Key**, not “the Hatch Key has been collected.” A `spawn` using this value can fire during that phase. The following phase, `300`, waits for the player to go down through the hatch.

`c03_1_Main` by itself is a flow/progression-variable name, not an FSM state. It will not select the whole first main-house section. The runtime only compares a resolved FSM's current state ID. It does not also check whether that controller belongs to the currently active chapter.

Important timing consequences:

- A spawn condition is a one-time trigger. Once it fires, leaving that state does not hide the enemies; use suspend/despawn rules for that.
- A checkpoint loaded after a phase has passed will not replay that phase just to satisfy your rule. A position trigger is usually better for an encounter that must work on revisits or later saves.
- Conditions are evaluated only after a group member is discovered. A phase that ends before its encounter scene loads can be missed. State and position on the same row must overlap in time.
- State `0` is deliberately omitted. In the live check, several future chapter controllers already reported `0` while another chapter was active. It is not a reliable “this chapter has started” condition.
- The CSV's phase hints are derived from native progression-flag names. They describe the gate associated with a phase; they are not a live inventory check, a permanent completion flag, or proof that every listed phase lasts long enough for an encounter.

## Useful starting points

These labels explain common rows from the CSV. “Waiting for” means the named milestone has not yet advanced this controller to its next phase. The owner GUID and state spelling come from assets; the plain-English wording interprets their gate flags. Test the chosen condition at the intended checkpoint before publishing an encounter.

| Area / phase | Story timing | Copy into State |
| --- | --- | --- |
| Guest house / 200 | Waiting to meet Mia | `a65f8f31-2527-4144-aa54-484479a1303c\|0\|200` |
| Guest house / 1000 | Waiting for fuse pickup | `a65f8f31-2527-4144-aa54-484479a1303c\|0\|1000` |
| Main house opening / 200 | Waiting for the Hatch Key | `2efaa57c-6ae1-4810-aeb1-c0d3b1642fd8\|0\|200` |
| Main house opening / 300 | Waiting to go through the floor hatch | `2efaa57c-6ae1-4810-aeb1-c0d3b1642fd8\|0\|300` |
| Main house opening / 600 | Waiting for the deputy to give the knife | `2efaa57c-6ae1-4810-aeb1-c0d3b1642fd8\|0\|600` |
| Garage / 900 | Waiting for the Jack garage battle to end | `2efaa57c-6ae1-4810-aeb1-c0d3b1642fd8\|0\|900` |
| Main house opening / 1100 | Waiting to enter the main hall | `2efaa57c-6ae1-4810-aeb1-c0d3b1642fd8\|0\|1100` |
| Main house / 100 | Waiting for the wooden statuette | `2505bf32-b0b3-43c8-8805-462d473a43f3\|0\|100` |
| Main house / 200 | Waiting to solve the shadow puzzle | `2505bf32-b0b3-43c8-8805-462d473a43f3\|0\|200` |
| Processing area / 600 | Waiting for the Jack battle to end | `2505bf32-b0b3-43c8-8805-462d473a43f3\|0\|600` |
| Main house exit / 700 | Waiting for all three dog heads to be placed | `2505bf32-b0b3-43c8-8805-462d473a43f3\|0\|700` |
| Yard / 100 | Waiting to enter the trailer | `52c9e905-9959-4afb-8533-9ad1c2383de6\|0\|100` |
| Old House / 300 | Waiting for the crank | `52c9e905-9959-4afb-8533-9ad1c2383de6\|0\|300` |
| Old House / 400 | Waiting for the Crow Key (`TalismanKey`) | `52c9e905-9959-4afb-8533-9ad1c2383de6\|0\|400` |
| Greenhouse / 1000 | Waiting for mutated Marguerite's battle to end | `52c9e905-9959-4afb-8533-9ad1c2383de6\|0\|1000` |
| Old House / 1100 | Waiting for the lantern | `52c9e905-9959-4afb-8533-9ad1c2383de6\|0\|1100` |
| Old House / 1200 | Waiting for the lantern-door puzzle | `52c9e905-9959-4afb-8533-9ad1c2383de6\|0\|1200` |
| Main house revisit / 40 | Waiting for the Snake Key | `bb3ee227-b4a5-44ae-b8ad-23a47f7fdd9f\|0\|40` |
| Main house revisit / 50 | Waiting for the two testing-area keycards | `bb3ee227-b4a5-44ae-b8ad-23a47f7fdd9f\|0\|50` |
| Testing area / 700 | Waiting for the battery to be installed | `bb3ee227-b4a5-44ae-b8ad-23a47f7fdd9f\|0\|700` |
| Barn / 815 | Waiting for the Fat Molded kill flag | `bb3ee227-b4a5-44ae-b8ad-23a47f7fdd9f\|0\|815` |
| Boathouse / 150 | Waiting to enter the boathouse | `4aa4d676-9b1c-40b9-ab97-c602ca2df7ff\|0\|150` |
| Boathouse / 500 | Waiting for mutated Jack's battle to end | `4aa4d676-9b1c-40b9-ab97-c602ca2df7ff\|0\|500` |
| Ship / 300 | Waiting for the videotape to be inserted | `b9945070-2181-432d-9621-c182f0cc5803\|0\|300` |
| Ship / 500 | Waiting to enter the elevator | `b9945070-2181-432d-9621-c182f0cc5803\|0\|500` |
| Ship / 600 | Waiting for the elevator to move | `b9945070-2181-432d-9621-c182f0cc5803\|0\|600` |
| Mines / 500 | Waiting to leave the salt mine | `74f1bc98-9572-488c-ad22-c2b47d31c5e8\|0\|500` |
| Finale / 100 | Waiting for the last boss battle to end | `0cd1594b-ed5d-4146-9bac-cd682a5250a8\|0\|100` |

When viewing raw Markdown, table pipes have escaping for formatting. The CSV's State cells and the standalone example above contain the exact unescaped strings to paste.

## Coverage and evidence

The CSV links each condition to `SceneFile`, `OwnerPath`, `OwnerGuid`, `FsmResource`, `StateName` and the native state ID. `GatePackage`, `GateFlags` and `GateFlagGuids` show the source of its phase hint. `CoreId` is zero because these selected controllers each have one flat root graph; nested graphs and other core layouts are excluded. IDs are scene identities, not session memory addresses.

| ChapterSection filter | Controller / area | Owning scene |
| --- | --- | --- |
| `c01` | `c01_MainFlow` / guest house | `leveldesign/fsm/chapter1/levelfsm_c01.scn.20` |
| `c03_1` | `c03_1_MainFlow` / dinner escape and garage | `leveldesign/fsm/chapter3/common_c03_1.scn.20` |
| `c03_2` | `c03_2_MainFlow` / main house and dog heads | `leveldesign/fsm/chapter3/common_c03_2.scn.20` |
| `c03_3` | `c03_3_MainFlow` / yard and Old House | `leveldesign/fsm/chapter3/common_c03_3.scn.20` |
| `c03_4` | `c03_4_MainFlow` / keycards and testing area | `leveldesign/fsm/chapter3/common_c03_4.scn.20` |
| `c03_4A`, `c03_4B`, `c03_4C` | Keycard and birthday-puzzle subflows | `leveldesign/fsm/chapter3/common_c03_4.scn.20` |
| `c03_5` | `c03_5_MainFlow` / boathouse and serum choice | `leveldesign/fsm/chapter3/common_c03_5.scn.20` |
| `c04_1` | `c04_1_MainFlow` / present-day ship | `leveldesign/fsm/chapter4/chapter4_1/levelfsm_c04_1.scn.20` |
| `c04_2` | `c04_2_MainFlow` / mine approach and salt mine | `leveldesign/fsm/chapter4/chapter4_2/levelfsm_c04_2.scn.20` |
| `c04_3` | `c04_3_Main` / finale | `leveldesign/fsm/chapter4/chapter4_3/levelfsm_c04_3.scn.20` |

Scene paths in this table omit the common `natives/stm/` prefix. The CSV includes it.

**Offline verification, 2026-10-09:** every exported owner and FSM binding was read from the BioRand baseline scene PAK. State names and IDs were read from the matching native RT FSM resource. The generator resolves action UIDs to that owner's scene overrides, reads the actual `GameFlowFlagPkg`, and resolves its flags in `globalvariables.uvar.2`. It exports metadata only. The standard root-core mapping was additionally checked in the running game for the chapter 3 controllers below.

**Live lookup verification, 2026-10-09:** a read-only scan found the eight chapter 3 owners through the same `via.fsm.Fsm` component type used by the runtime. **All 81 chapter 3 CSV conditions** resolved on those owners at core 0: native `hasState`/`hasStateFullName` accepted the state, and the corresponding `queryStateID` result matched its CSV `StateIdHex`. There were zero lookup errors or ID mismatches.

| Controller | CSV conditions checked live |
| --- | --- |
| `c03_1_MainFlow` | All 11 |
| `c03_2_MainFlow` | All 6 |
| `c03_3_MainFlow` | All 17 |
| `c03_4_MainFlow` | All 25 |
| `c03_4A_MainFlow` | 1 |
| `c03_4B_MainFlow` | 1 |
| `c03_4C_MainFlow` | All 5 |
| `c03_5_MainFlow` | All 15 |

At that checkpoint `c03_2_MainFlow` actually reported `400`; `c03_1_MainFlow` reported `After`, and future main controllers reported `0`. This verified state lookup/IDs and recorded current states; it did not test progression through every row, every scene's streaming lifetime, or a generated encounter. The remaining 46 CSV rows in chapters 1 and 4 have offline asset evidence only. `get_ResourcePath()` returned empty strings in this live session, so an empty runtime resource path must not be used to reject an otherwise valid owner/state lookup.

Deliberate omissions:

- All state `0`, `End`, `After`, `after` and `ActiveOff` entries, which can represent inactive/default or lifecycle states.
- `c03_4` state `1050`, because the resource contains two different states with that same name; a name-based rule cannot safely distinguish them.
- `c03_4` state `1090`, for which this extractor could not link a progression gate.
- `c04_2` states `600` and `700`, whose gate flags could not be resolved in the examined global-variable table.
- DLC, flashbacks, arbitrary object states and nested/parallel FSM graphs. No synthetic intermediate numbers are added; for example, `c03_2` has no state `300`.

## Adding or verifying an entry

Contributors can request an entry by describing the intended encounter, chapter, room and story milestone, and providing a checkpoint or reproducible route to it. State whether the enemies should appear before that milestone, after it, or whenever the room is revisited. You do not need to discover a native GUID yourself.

A maintainer should establish the following evidence before adding a copyable condition:

1. Read the owning scene and record the **scene GameObject GUID**, owner path and actual `via.fsm.Fsm` resource/SceneData binding. A Uvar GUID, FSM `InstanceGuid` or live memory address is not a substitute.
2. Link the exact state name and native state ID to that resource and its applicable core. Reject ambiguous names. Read the owner's action overrides and progression gates to establish what the state means; do not infer a milestone from its number alone. The generator below handles the supported flat main-flow controllers; other layouts require separate inspection.
3. With that owner loaded, verify `hasState` or `hasStateFullName`, then verify the corresponding native query returns the expected state ID on the intended core. Use the same owner-GUID/core/name lookup as the runtime. Record asset-only entries separately when live lookup is not yet verified.
4. Check a checkpoint before, during and after the intended phase, including when the encounter's members become available. Confirm when the condition is true, whether it can be missed, and what happens after save/load and scene re-entry. Test the generated encounter before calling its timing validated.

Record which checks were actually performed. A successful lookup establishes that a condition can resolve; only the checkpoint and encounter checks establish that it triggers at the intended time.

## Regenerating the CSV

Maintainers need .NET 10, the repository, the same BioRand baseline PAK used for generation, and the unmodified RT base PAK from their own game installation. Contributors using the reference do not need these tools. Run this from the repository root, replacing the two placeholder paths:

```powershell
dotnet run --file tools/generate-spawn-group-states.cs -- `
  --scene-pak "<baseline>/biorand-re7.pak" `
  --resource-pak "<RE7 RT installation>/re_chunk_000.pak" `
  --output docs/enemies/spawn_group_states.csv
```

The [generator](../../tools/generate-spawn-group-states.cs) pins REE 1.5.6, reads the input PAKs without modifying them, and emits only the CSV. It rejects unsupported graph shapes and ambiguous action overrides, and reports excluded state mappings. Review changes against this document's descriptions and live-lookup coverage before updating the published reference. Updating the CSV does not establish new live validation, and no game binaries or local dumps should be committed with it.
