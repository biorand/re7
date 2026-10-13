using System.Collections.Immutable;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Patches;

[ExportMod(Name = "DLC Weapon Lab", FileName = "dlc-weapon-lab", Version = "0.1.0",
    Author = "BioRand", Description = "Experimental campaign weapon adapters. Not certified for a saved playthrough.")]
internal sealed class DlcWeaponLabPatch(IPatchContext context) : IPatch {
    internal static ImmutableArray<string> RequiredAssetPaths { get; } = [.. DlcCampaignWeapons.RequiredAssetPaths
        .Concat(DlcGrenadeWeapons.RequiredAssetPaths)
        .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal)];

    public void Apply() {
        if (!context.ExportingMod) return;
        var assets = RequiredAssetPaths.Select(path => (Path: path, Data: context.GetFile(path))).ToArray();
        var missing = assets.Where(a => a.Data == null).Select(a => a.Path).ToArray();
        if (missing.Length != 0)
            throw new RandomizerUserException($"DLC Weapon Lab requires installed Not a Hero and End of Zoe assets. Missing {missing.Length} resource(s), first: {missing[0]}");
        foreach (var asset in assets) context.SetFile(asset.Path, asset.Data!);
        new DlcWeaponImporter(context).Apply(DlcWeaponCatalog.Weapons.Where(w => w.IsLabCandidate).ToArray());
    }

    internal static RszScene AdaptPrefab(RszScene scene, RszTypeRepository types, DlcWeaponSource weapon)
        => DlcWeaponImporter.AdaptPrefab(scene, types, weapon);
}
