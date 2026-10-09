using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Patches;

[ExportMod(Name = "DLC Weapon Lab", FileName = "dlc-weapon-lab", Version = "0.1.0",
    Author = "BioRand", Description = "Experimental campaign weapon adapters. Not certified for a saved playthrough.")]
internal sealed class DlcWeaponLabPatch(IPatchContext context) : IPatch {
    public void Apply() {
        if (!context.ExportingMod) return;
        new DlcWeaponImporter(context).Apply(DlcWeaponCatalog.Weapons.Where(w => w.IsLabCandidate).ToArray());
    }

    internal static RszScene AdaptPrefab(RszScene scene, RszTypeRepository types, DlcWeaponSource weapon)
        => DlcWeaponImporter.AdaptPrefab(scene, types, weapon);
}
