using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;

namespace Biohazard.BioRand.RE7.Patches;

internal sealed class DlcCampaignWeaponPatch(IPatchContext context) : IPatch {
    public void Apply() {
        if (!DlcCampaignWeapons.IsEnabled(context)) return;
        var assets = DlcCampaignWeapons.RequiredAssetPaths.Select(path => (Path: path, Data: context.GetFile(path))).ToArray();
        var missing = assets.Where(a => a.Data == null).Select(a => a.Path).ToArray();
        if (missing.Length != 0)
            throw new RandomizerUserException($"DLC campaign weapons require an updated baseline built with setup --dlc-weapons from an installation containing Not a Hero and End of Zoe. Missing {missing.Length} resource(s), first: {missing[0]}");
        foreach (var asset in assets) {
            if (context is FileRepository repository) repository.SetAdditionalOutputAssetFile(asset.Path, asset.Data!);
            else context.SetFile(asset.Path, asset.Data!);
        }
        new DlcWeaponImporter(context).Apply(DlcCampaignWeapons.Sources, isolateParameters: true);
    }
}
