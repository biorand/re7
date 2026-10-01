# BioRand 7

[![.NET](https://github.com/juliangrtz/re7/actions/workflows/ci.yml/badge.svg)](https://github.com/juliangrtz/re7/actions/workflows/ci.yml) [![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/juliangrtz/re7/issues) 


<p align="center">
    <img src="assets/logo.png" alt="drawing" width="400"/>
</p>
<br>

BioRand 7 is a [cloud randomizer](https://beta-re7.biorand.net/) and mod-generation toolkit for [Resident Evil 7 Biohazard](https://store.steampowered.com/app/418370/Resident_Evil_7_Biohazard/) based on the [BioRand infrastructure](https://github.com/biorand).

This repository contains the .NET randomizer library, command-line tools, data-generation utilities, tests, REFramework Lua scripts, and reverse-engineering notes that support it.

BioRand 7 is a fan-made project and is not affiliated with or endorsed by Capcom. You need a legally owned copy of Resident Evil 7 including all DLCs to use game-derived inputs locally.

## Status

BioRand 7 is in active beta development. Item, weapon, inventory, recipe, and several progression-safety systems are already implemented; enemy and chapter-related work is still especially sensitive and should be tested carefully.

For current design notes and planned work, see:

- [Roadmap](docs/Roadmap.MD)
- [Technical notes](docs/Notes.MD)
- [Enemy spawning notes](docs/enemies/enemy_spawning.md)
- [Key item route graph](docs/key_item_route_graph.png)
- [Flags, triggers, stats notes](docs/UvarVariables.MD)

## Features

<a href="docs/key_item_route_graph.png">
    <img src="docs/key_item_route_graph.png" style="width: 200px;" align="right" alt="Key item rando graph"/>
</a>

- Generates RE7 randomizer output as a patch PAK, Fluffy Mod Manager ZIP, or extracted `natives/` folder.
- Supports seeded generation and JSON configuration profiles.
- Randomizes items, key item locations, item drops, bird cages, starting inventory, inventory stack limits, and crafting recipes.
- Emits an offline HTML key-item spoiler with numbered pickup markers on RE7 floor-plan images.
- Includes weapon stat randomization for damage, ammo capacity, and reload speed.
- Includes enemy randomization options for enemy classes, multipliers, placement, health, speed, damage, and scale.
- Adds REFramework artifacts when features require runtime support.
- Provides standalone mod export commands for bundled optional mods.
- Uses embedded, spreadsheet-derived, and generated data so behavior can be tested without a local RE7 install.

## Requirements

For players, use the Steam Windows RT/DX12 version of RE7 with its DLC installed. The older `dx11_non-rt` branch is unsupported. The SDK and Git requirements below apply to building the tools yourself.

## Installing a generated seed

1. Close RE7 and back up your saves before changing an installation. Start a new game for a new seed or changed progression settings.
2. Download the seed's **Patch** ZIP and **Additional Assets** ZIP, if the generator lists one. Extract their contents into the folder containing `re7.exe`; placing the ZIP files there does not install them. Keep the directory structure, including `reframework/autorun/BioRand7.lua`, its `BioRand7/` modules, and `reframework/data/BioRand7/config.json`.
3. When the output includes REFramework scripts, install the RE7 RT version of [REFramework](https://github.com/praydog/REFramework/releases) so its `dinput8.dll` sits next to `re7.exe`. Most randomizer profiles need this runtime, including randomized weapons and extra enemies. The generated ZIP normally supplies the BioRand scripts, not REFramework itself.
4. For the Patch installation, install every Additional Assets download listed for that seed. Missing assets can cause infinite loading. Reuse an installed asset pack only when its version matches the generated download.
5. Alternatively, enable the **Fluffy Mod** ZIP in Fluffy Mod Manager. Newly generated Fluffy archives include the shared assets, so no separate Additional Assets installation is needed. Follow the same REFramework requirement. Use one installation method and one seed at a time; disable the old Fluffy seed or remove the previously installed BioRand patch PAK before switching methods.

Older Fluffy archives still require their matching Additional Assets download to be extracted into the game folder. If a Fluffy installation hangs when entering particular rooms, first check this dependency, the RT/DX12 game version, and that only one BioRand seed is installed. If the hang persists, provide the seed/config, exact room or transition, Fluffy version and `Data/Log.txt`, and whether the same seed loads using the Patch installation after disabling the Fluffy mod.

When upgrading, disable the previous seed and remove its BioRand Lua entrypoint/module directory before installing the replacement, including when the new profile does not require scripts. Remove any legacy BioRand managed plugin or autorun script from an older C# installation so two runtimes cannot run together; preserve unrelated mods and plugins. Keep the seed's config, logs, build version, and optional spoilers for support.

To uninstall, disable the Fluffy mod or remove the BioRand patch PAKs supplied by the seed and asset downloads, plus `reframework/autorun/BioRand7.lua`, `reframework/autorun/BioRand7/`, and `reframework/data/BioRand7/`. Remove only files installed by BioRand; do not remove vanilla PAKs or another mod's runtime.

Chapter shuffling is unavailable in public profiles until return routes are validated. Old profiles with it enabled are rejected with an explanation. Generation uses the CSV snapshot embedded in the application by default; the advanced spreadsheet-refresh option is for development and can change an existing seed's results.

## Developer requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Git with [Git LFS](https://git-lfs.com/)
- A local Resident Evil 7 install with all DLCs for local setup/mod generation workflows
- Windows is recommended for game-related workflows

Large embedded assets, including `src/Biohazard.BioRand.RE7/_Data/silver_birthday_patches.zip`, are stored with Git LFS. Install Git LFS before cloning when possible:

```powershell
git lfs install
```

For an existing clone, fetch the LFS payloads before building or running tests:

```powershell
git lfs pull
```

## Build And Test

From the repository root:

```powershell
dotnet restore .\biorand-re7.sln
dotnet build .\biorand-re7.sln --no-restore
dotnet test .\biorand-re7.sln --no-build --verbosity normal
```

The REFramework runtime also has executable Lua tests, independent of the game and baseline PAK:

```powershell
lua tests/lua/run.lua
```

CI runs these with Lua 5.4. See [REFramework Lua validation](docs/reframework-lua.md) for the runtime API rules and in-game checks.

CI tests both Debug and Release. Pushes to `master` and manually triggered release checks require `BIORAND_RE7_TEST_PAK_URL` and `BIORAND_RE7_TEST_PAK_SHA256`; a run without PAK-backed tests is not release validation. Fork pull requests can run the tests that do not need game data.

The CLI's `generate -o` destination includes its runtime files: `.pak` writes scripts/config beside the PAK, a directory receives the loose game files and runtime, and `.zip` writes the Fluffy archive with companion downloads beside it. Logs and spoilers stay with that destination. `setup` and `setup --full` both extract the baseline required for generation.

## Benchmarks

Randomizer throughput benchmarks live in `src/Biohazard.BioRand.RE7.Benchmarks/` and use the embedded baseline PAK, `%USERPROFILE%\.biorand\biorand-re7.pak`, or a path supplied through `BIORAND_RE7_BENCHMARK_PAK`.

Run them from the repository root in Release mode:

```powershell
dotnet run -c Release --project .\src\Biohazard.BioRand.RE7.Benchmarks\Biohazard.BioRand.RE7.Benchmarks.csproj
```

To run a single scenario:

```powershell
dotnet run -c Release --project .\src\Biohazard.BioRand.RE7.Benchmarks\Biohazard.BioRand.RE7.Benchmarks.csproj -- --filter *DefaultProfile*
```

The `RealisticProfile` scenario uses the checked-in profile under `src/Biohazard.BioRand.RE7.Benchmarks/Profiles/`. Benchmarks disable dynamic Google Sheets downloads by default; set `BIORAND_RE7_BENCHMARK_DOWNLOAD_DATA=1` to include that external fetch cost.

## Data Workflows

Some runtime data is embedded under `src/Biohazard.BioRand.RE7/_Data/`. Changes there affect generated seeds, not only tests.

Refresh dynamic CSV data from the [Google Sheets spreadsheets](https://docs.google.com/spreadsheets/d/1YNdX9LWrhh6KDKd8Mx7JpTCMq8XY8u6BfX20YYNx9jk):

```powershell
dotnet run --project .\src\biorand-re7\biorand-re7.csproj -- update
```

Run data generators:

```powershell
dotnet run --project .\src\Biohazard.BioRand.RE7.DataGen\Biohazard.BioRand.RE7.DataGen.csproj -- generate config
dotnet run --project .\src\Biohazard.BioRand.RE7.DataGen\Biohazard.BioRand.RE7.DataGen.csproj -- generate areas item_placements item_definitions weapon_definitions enemies
dotnet run --project .\src\Biohazard.BioRand.RE7.DataGen\Biohazard.BioRand.RE7.DataGen.csproj -- generate area_scene_targets -f Json
dotnet run --project .\src\Biohazard.BioRand.RE7.DataGen\Biohazard.BioRand.RE7.DataGen.csproj -- rsz-to-cs app.TypeName --with-enums
```

Generated files are written to `GeneratedFiles/`. Some generators also copy outputs into `_Data/`.
The key item route graph image is refreshed into `docs/key_item_route_graph.png` when the DataGen project builds.

## Repository Layout

```text
src/Biohazard.BioRand.RE7/                    Core randomizer library
src/biorand-re7/                              Command-line app
src/Biohazard.BioRand.RE7.Benchmarks/         BenchmarkDotNet throughput benchmarks
src/Biohazard.BioRand.RE7.DataGen/            Data and code generators
src/Biohazard.BioRand.RE7/_Data/reframework/  REFramework Lua autorun scripts
src/Biohazard.BioRand.RE7.Tests/              xUnit regression tests
docs/                                         Notes, roadmap, and research docs
assets/                                       Project assets and bundled mod assets
```

## Contributing

Contributions are welcome, especially focused bug fixes, tests, documentation, data corrections, and carefully scoped randomizer improvements.

Start with [CONTRIBUTING.md](CONTRIBUTING.md). For issues, use the existing GitHub templates and attach the randomizer ZIP output when reporting crashes or softlocks. Do not attach full vanilla game PAKs, game install dumps, local credentials, or private files.

## Acknowledgements

BioRand 7 would not be possible without a few amazing people and tools:

- [IntelOrca](https://github.com/IntelOrca)'s [BioRand infrastructure](https://github.com/biorand)
- [Battlezone](https://github.com/seifhassine)'s [REasy](https://github.com/seifhassine/REasy)
- [kagenocookie](https://github.com/kagenocookie)'s [REE Content Editor](https://github.com/kagenocookie/REE-Content-Editor)
- [praydog](https://github.com/praydog)'s [RE Framework](https://github.com/praydog/REFramework)
- [alphaZomega](https://github.com/alphazolam)'s many contributions to RE modding
- [Silver](https://github.com/SilverEzredes)'s spiffy patches for the Jack's 55th Birthday skills

## License

This project is licensed under the [MIT License](LICENSE).
